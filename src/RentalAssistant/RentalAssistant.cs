using ChromaDB.VectorData;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

namespace RentalAssistant;

/// <summary>
/// A Semantic Kernel assistant that answers customers from the documents of the shop stored in Chroma.
/// </summary>
public sealed class RentalAssistant : IDisposable
{
    public const string CollectionName = "bike-rental";

    // The search runs inside the prompt: the passages found in Chroma are written into the prompt before it reaches the model.
    private const string Prompt = """
        You are the assistant of Lakeside Bike Rental. Answer the question of the customer from the passages below.
        If they do not contain the answer, say that you do not know. Answer in at most three sentences.

        {{#with (Shop-GetTextSearchResults question)}}
        {{#each this}}
        Passage: {{Name}}
        {{Value}}
        -----------------
        {{/each}}
        {{/with}}

        Question: {{question}}
        """;

    private readonly TextSearchStore<string> _store;
    private readonly KernelFunction _answer;

    private RentalAssistant(TextSearchStore<string> store, Kernel kernel, KernelFunction answer)
    {
        _store = store;
        Kernel = kernel;
        _answer = answer;
    }

    public Kernel Kernel { get; }

    /// <summary>
    /// Stores the documents of the shop in Chroma and creates the assistant.
    /// </summary>
    /// <param name="chatClient">The chat model.</param>
    /// <param name="vectorStore">The Chroma vector store, with the embedding generator of the embedding model.</param>
    /// <param name="embeddingDimensions">The number of dimensions of the embeddings.</param>
    public static async Task<RentalAssistant> CreateAsync(IChatClient chatClient, ChromaVectorStore vectorStore, int embeddingDimensions, CancellationToken cancellationToken = default)
    {
        // The store of Semantic Kernel for retrieval, in a Chroma collection. Searches see only the customer documents,
        // and the source id is the key, so that storing the documents again replaces them.
        var store = new TextSearchStore<string>(vectorStore, CollectionName, embeddingDimensions, new TextSearchStoreOptions
        {
            SearchNamespace = Documents.CustomersNamespace,
            UseSourceIdAsPrimaryKey = true,
        });
        await store.UpsertDocumentsAsync(Documents.All, cancellationToken: cancellationToken);

        var builder = Kernel.CreateBuilder();
        builder.Services.AddChatClient(chatClient);
        builder.Plugins.Add(store.CreateWithGetTextSearchResults("Shop"));
        var kernel = builder.Build();

        var answer = kernel.CreateFunctionFromPrompt(
            new PromptTemplateConfig(Prompt) { TemplateFormat = HandlebarsPromptTemplateFactory.HandlebarsTemplateFormat },
            new HandlebarsPromptTemplateFactory());

        return new RentalAssistant(store, kernel, answer);
    }

    public async Task<string> AskAsync(string question, CancellationToken cancellationToken = default)
    {
        var result = await _answer.InvokeAsync(Kernel, new KernelArguments { ["question"] = question }, cancellationToken);
        return result.ToString();
    }

    public void Dispose() => _store.Dispose();
}
