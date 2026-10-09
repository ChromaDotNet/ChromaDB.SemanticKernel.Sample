// Copyright (c) Microsoft. All rights reserved.

using Azure.Identity;
using CommunityToolkit.VectorData.Chroma;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using static GettingStartedWithTextSearch.ChromaVectorStoreFixture;

namespace GettingStartedWithTextSearch;

/// <summary>
/// This example shows how to create a <see cref="ITextSearch"/> from a
/// <see cref="ChromaVectorStore"/>.
/// </summary>
/// <remarks>
/// To run this sample, you need a local instance of Docker running, since the associated fixture will start a Chroma container in the local docker instance.
/// </remarks>
[Collection("ChromaVectorStoreCollection")]
public class Step5_Search_With_Chroma(ITestOutputHelper output, ChromaVectorStoreFixture fixture) : BaseTest(output)
{
    /// <summary>
    /// Show how to create a <see cref="VectorStoreTextSearch{TRecord}"/> and use it to perform a search.
    /// </summary>
    [Fact]
    public async Task UsingChromaVectorStoreRecordTextSearchAsync()
    {
        // Use embedding generation service and record collection for the fixture.
        var collection = fixture.VectorStoreRecordCollection;

        // Create a text search instance using the Chroma vector store.
        var textSearch = new VectorStoreTextSearch<DataModel>(collection);

        // Search and return results as TextSearchResult items
        var query = "What is the Semantic Kernel?";
        KernelSearchResults<TextSearchResult> textResults = await textSearch.GetTextSearchResultsAsync(query, new() { Top = 2, Skip = 0 });
        Console.WriteLine("\n--- Text Search Results ---\n");
        await foreach (TextSearchResult result in textResults.Results)
        {
            Console.WriteLine($"Name:  {result.Name}");
            Console.WriteLine($"Value: {result.Value}");
            Console.WriteLine($"Link:  {result.Link}");
        }
    }

    /// <summary>
    /// Show how to create a default <see cref="KernelPlugin"/> from an <see cref="ITextSearch"/> and use it to
    /// add grounding context to a Handlebars prompt.
    /// </summary>
    [Fact]
    public async Task RagWithChromaVectorStoreTextSearchAsync()
    {
        // Create a kernel with Azure OpenAI chat completion
        IKernelBuilder kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.AddAzureOpenAIChatCompletion(
                deploymentName: TestConfiguration.AzureOpenAI.ChatDeploymentName,
                endpoint: TestConfiguration.AzureOpenAI.Endpoint,
                credentials: new AzureCliCredential());
        Kernel kernel = kernelBuilder.Build();

        // Use embedding generation service and record collection for the fixture.
        var collection = fixture.VectorStoreRecordCollection;

        // Create a text search instance using the Chroma vector store.
        var textSearch = new VectorStoreTextSearch<DataModel>(collection);

        // Build a text search plugin with vector store search and add to the kernel
        var searchPlugin = textSearch.CreateWithGetTextSearchResults("SearchPlugin");
        kernel.Plugins.Add(searchPlugin);

        // Invoke prompt and use text search plugin to provide grounding information
        var query = "What is the Semantic Kernel?";
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
        KernelArguments arguments = new() { { "query", query } };
        HandlebarsPromptTemplateFactory promptTemplateFactory = new();
        Console.WriteLine(await kernel.InvokePromptAsync(
            promptTemplate,
            arguments,
            templateFormat: HandlebarsPromptTemplateFactory.HandlebarsTemplateFormat,
            promptTemplateFactory: promptTemplateFactory
        ));
    }

    /// <summary>
    /// Show how to create a default <see cref="KernelPlugin"/> from an <see cref="VectorStoreTextSearch{TRecord}"/> and use it with
    /// function calling to have the LLM include grounding context in it's response.
    /// </summary>
    [Fact]
    public async Task FunctionCallingWithChromaVectorStoreTextSearchAsync()
    {
        // Create a kernel with Azure OpenAI chat completion
        IKernelBuilder kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.AddAzureOpenAIChatCompletion(
                deploymentName: TestConfiguration.AzureOpenAI.ChatDeploymentName,
                endpoint: TestConfiguration.AzureOpenAI.Endpoint,
                credentials: new AzureCliCredential());
        Kernel kernel = kernelBuilder.Build();

        // Use embedding generation service and record collection for the fixture.
        var collection = fixture.VectorStoreRecordCollection;

        // Create a text search instance using the Chroma vector store.
        var textSearch = new VectorStoreTextSearch<DataModel>(collection);

        // Build a text search plugin with vector store search and add to the kernel
        var searchPlugin = textSearch.CreateWithGetTextSearchResults("SearchPlugin");
        kernel.Plugins.Add(searchPlugin);

        // Invoke prompt and use text search plugin to provide grounding information
        AzureOpenAIPromptExecutionSettings settings = new() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };
        KernelArguments arguments = new(settings);
        Console.WriteLine(await kernel.InvokePromptAsync("What is the Semantic Kernel?", arguments));
    }
}
