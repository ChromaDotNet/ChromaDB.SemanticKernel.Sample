using System.Text.Json;
using GettingStartedWithTextSearch;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using static GettingStartedWithTextSearch.ChromaVectorStoreFixture;

namespace ChromaDB.SemanticKernel.Sample.Tests;

/// <summary>
/// The fixture of <see cref="Step5_Search_With_Chroma"/>, with the embeddings of <see cref="WordEmbeddingGenerator"/>.
/// </summary>
public sealed class WordEmbeddingChromaVectorStoreFixture : ChromaVectorStoreFixture
{
    protected override IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator() => new WordEmbeddingGenerator(1536);
}

/// <summary>
/// Runs <see cref="Step5_Search_With_Chroma"/> with the embeddings of <see cref="WordEmbeddingGenerator"/>
/// and a chat model that only records what it receives.
/// </summary>
public sealed class TextSearchTests(WordEmbeddingChromaVectorStoreFixture fixture) : IClassFixture<WordEmbeddingChromaVectorStoreFixture>
{
    private const string Query = "What is the Semantic Kernel?";

    // The line that shares the most words with the question.
    private const string KernelLine = "The kernel is the central component of Semantic Kernel.";

    [Fact]
    public async Task The_search_returns_the_records_with_their_key_text_and_link()
    {
        var textSearch = new VectorStoreTextSearch<DataModel>(fixture.VectorStoreRecordCollection);

        KernelSearchResults<TextSearchResult> textResults = await textSearch.GetTextSearchResultsAsync(Query, new() { Top = 2, Skip = 0 });
        var results = await textResults.Results.ToListAsync();

        Assert.Equal(2, results.Count);
        Assert.StartsWith(KernelLine, results[0].Value);
        Assert.All(results, result =>
        {
            Assert.True(Guid.TryParse(result.Name, out _));
            Assert.Equal($"guid://{result.Name}", result.Link);
        });
    }

    [Fact]
    public async Task The_Handlebars_prompt_receives_the_search_results()
    {
        var model = new RecordingChatClient();
        IKernelBuilder kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.Services.AddSingleton<IChatCompletionService>(model.AsChatCompletionService());
        Kernel kernel = kernelBuilder.Build();
        kernel.Plugins.Add(new VectorStoreTextSearch<DataModel>(fixture.VectorStoreRecordCollection).CreateWithGetTextSearchResults("SearchPlugin"));

        string promptTemplate = """
            {{#with (SearchPlugin-GetTextSearchResults query)}}
              {{#each this}}
                Name: {{Name}}
                Value: {{Value}}
                Link: {{Link}}
                -----------------
              {{/each}}
            {{/with}}

            {{query}}

            Include citations to the relevant information where it is referenced in the response.
            """;
        await kernel.InvokePromptAsync(
            promptTemplate,
            new() { { "query", Query } },
            templateFormat: HandlebarsPromptTemplateFactory.HandlebarsTemplateFormat,
            promptTemplateFactory: new HandlebarsPromptTemplateFactory());

        Assert.Contains($"Value: {KernelLine}", model.LastMessagesText);
        Assert.Contains("Link: guid://", model.LastMessagesText);
        Assert.Contains(Query, model.LastMessagesText);
    }

    [Fact]
    public async Task With_function_calling_the_search_results_reach_the_model()
    {
        var model = new FunctionCallingChatClient(new() { ["query"] = Query });
        IKernelBuilder kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.Services.AddSingleton<IChatClient>(model.AsBuilder().UseKernelFunctionInvocation().Build());
        Kernel kernel = kernelBuilder.Build();
        kernel.Plugins.Add(new VectorStoreTextSearch<DataModel>(fixture.VectorStoreRecordCollection).CreateWithGetTextSearchResults("SearchPlugin"));

        await kernel.InvokePromptAsync(Query, new(new PromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }));

        Assert.EndsWith("GetTextSearchResults", model.CalledToolName);
        var functionResult = Assert.Single(model.LastMessages.SelectMany(message => message.Contents).OfType<Microsoft.Extensions.AI.FunctionResultContent>());
        // The model receives the result of the function as JSON.
        Assert.Contains(KernelLine, JsonSerializer.Serialize(functionResult.Result));
    }
}
