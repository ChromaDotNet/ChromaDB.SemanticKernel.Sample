using Agents;
using ChromaDB.Client;
using ChromaDB.VectorData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Data;

namespace ChromaDB.SemanticKernel.Sample.Tests;

/// <summary>
/// Runs <see cref="ChatCompletion_Rag_Chroma"/> with the embeddings of <see cref="WordEmbeddingGenerator"/>
/// and a <see cref="RecordingChatClient"/> as the chat model.
/// </summary>
public sealed class ChatCompletionRagTests(ChromaFixture fixture) : IClassFixture<ChromaFixture>
{
    private const int EmbeddingDimensions = 64;

    [Fact]
    public async Task The_document_that_answers_each_question_reaches_the_model()
    {
        using var httpClient = new HttpClient();
        using var vectorStore = CreateVectorStore(httpClient);
        using var textSearchStore = new TextSearchStore<string>(vectorStore, collectionName: "FinancialData", vectorDimensions: EmbeddingDimensions);
        await textSearchStore.UpsertTextAsync(ChatCompletion_Rag_Chroma.GetSampleDocuments().Select(document => document.Text!));
        var (agent, model) = CreateAgent();
        ChatHistoryAgentThread agentThread = new();
        agentThread.AIContextProviders.Add(new TextSearchProvider(textSearchStore));

        await agent.InvokeAsync("Where is Contoso based?", agentThread).FirstAsync();
        Assert.Contains("headquarters in Paris", model.LastMessagesText);

        await agent.InvokeAsync("What was its expenses for 2022?", agentThread).FirstAsync();
        Assert.Contains("Expenses EUR 162 000 000", model.LastMessagesText);
    }

    [Fact]
    public async Task Only_the_documents_of_the_search_namespace_reach_the_model_with_their_source()
    {
        using var httpClient = new HttpClient();
        using var vectorStore = CreateVectorStore(httpClient);
        using var textSearchStore = new TextSearchStore<string>(vectorStore, collectionName: "FinancialDataWithCitations", vectorDimensions: EmbeddingDimensions, new() { SearchNamespace = "group/g2" });
        await textSearchStore.UpsertDocumentsAsync(ChatCompletion_Rag_Chroma.GetSampleDocuments());
        var (agent, model) = CreateAgent();
        ChatHistoryAgentThread agentThread = new();
        agentThread.AIContextProviders.Add(new TextSearchProvider(textSearchStore));

        await agent.InvokeAsync("What was the income of Contoso for 2023", agentThread).FirstAsync();

        Assert.Contains("SourceDocName: Contoso 2023 Financial Report", model.LastMessagesText);
        Assert.Contains("SourceDocLink: https://www.consoso.com/reports/2023.pdf", model.LastMessagesText);

        // The report of 2024 is only in the group/g1 namespace, so not even a question about 2024 finds it.
        await agent.InvokeAsync("What was the income of Contoso for 2024", agentThread).FirstAsync();
        Assert.DoesNotContain("Contoso 2024 Financial Report", model.LastMessagesText);
    }

    private ChromaVectorStore CreateVectorStore(HttpClient httpClient)
        => new(new ChromaClient(new ChromaConfigurationOptions(fixture.Endpoint), httpClient), new() { EmbeddingGenerator = new WordEmbeddingGenerator(EmbeddingDimensions) });

    private static (ChatCompletionAgent Agent, RecordingChatClient Model) CreateAgent()
    {
        var model = new RecordingChatClient();
        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.Services.AddSingleton<IChatCompletionService>(model.AsChatCompletionService());
        return (new ChatCompletionAgent { Name = "FriendlyAssistant", Instructions = "You are a friendly assistant", Kernel = kernelBuilder.Build() }, model);
    }
}
