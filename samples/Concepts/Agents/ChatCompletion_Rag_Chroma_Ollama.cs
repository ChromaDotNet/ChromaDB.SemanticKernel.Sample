// Copyright (c) Microsoft. All rights reserved.

using ChromaDB.Client;
using ChromaDB.VectorData;
using Memory.VectorStoreFixtures;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Data;
using OllamaSharp;

namespace Agents;

#pragma warning disable SKEXP0130 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

/// <summary>
/// Demonstrate creation of <see cref="ChatCompletionAgent"/> and
/// adding simple retrieval augmented generation (RAG) capabilities to it, with the documents stored in Chroma,
/// and a chat model and an embedding model that run locally in Ollama.
/// </summary>
/// <remarks>
/// This is <see cref="ChatCompletion_Rag_Chroma"/> with the models of <see cref="TestConfiguration.Ollama"/> instead of Azure OpenAI.
///
/// To run this sample, you need a local instance of Docker running, since the associated fixture will try and start a Chroma container in the local docker instance.
/// You also need Ollama with the models: the compose file of the repository runs it and downloads them.
/// </remarks>
public class ChatCompletion_Rag_Chroma_Ollama(ITestOutputHelper output, VectorStoreChromaContainerFixture chromaFixture) : BaseTest(output), IClassFixture<VectorStoreChromaContainerFixture>
{
    private const string AgentName = "FriendlyAssistant";
    private const string AgentInstructions = "You are a friendly assistant";

    /// <summary>
    /// Shows how to do Retrieval Augmented Generation (RAG) with some basic text strings.
    /// </summary>
    [Fact]
    public async Task UseChatCompletionAgentWithBasicRag()
    {
        using IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = new OllamaApiClient(
            uriString: TestConfiguration.Ollama.Endpoint,
            defaultModel: TestConfiguration.Ollama.EmbeddingModelId);

        // Initialize the Chroma docker container via the fixtures.
        await chromaFixture.ManualInitializeAsync();

        // Create a vector store to store our documents.
        // Note that the embedding generator provided here must be able to generate embeddings matching the
        // number of dimensions configured for the TextSearchStore below.
        using var httpClient = new HttpClient();
        using var vectorStore = new ChromaVectorStore(
            new ChromaClient(new ChromaConfigurationOptions("http://localhost:8000"), httpClient),
            new() { EmbeddingGenerator = embeddingGenerator });

        // Create a store that uses a built in schema for storing text documents
        // and provides easy upload and search capabilities.
        // The data is stored in the `FinancialData` collection, with the dimensions of the embedding model.
        using var textSearchStore = new TextSearchStore<string>(vectorStore, collectionName: "FinancialData", vectorDimensions: TestConfiguration.Ollama.EmbeddingDimensions);

        // Upsert documents into the store.
        await textSearchStore.UpsertTextAsync(
            [
                "The financial results of Contoso Corp for 2024 is as follows:\nIncome EUR 154 000 000\nExpenses EUR 142 000 000",
                "The financial results of Contoso Corp for 2023 is as follows:\nIncome EUR 174 000 000\nExpenses EUR 152 000 000",
                "The financial results of Contoso Corp for 2022 is as follows:\nIncome EUR 184 000 000\nExpenses EUR 162 000 000",
                "The Contoso Corporation is a multinational business with its headquarters in Paris. The company is a manufacturing, sales, and support organization with more than 100,000 products.",
                "The financial results of AdventureWorks for 2021 is as follows:\nIncome USD 223 000 000\nExpenses USD 210 000 000",
                "AdventureWorks is a large American business that specializes in adventure parks and family entertainment.",
            ]);

        // Create our agent.
        Kernel kernel = Kernel.CreateBuilder()
            .AddOllamaChatCompletion(modelId: TestConfiguration.Ollama.ModelId, endpoint: new Uri(TestConfiguration.Ollama.Endpoint))
            .Build();
        ChatCompletionAgent agent =
            new()
            {
                Name = AgentName,
                Instructions = AgentInstructions,
                Kernel = kernel,
            };

        // Create a thread for the agent.
        ChatHistoryAgentThread agentThread = new();

        // Create a text search provider that can automatically search the vector store
        // for documents that match the user's query and inject them into the agent's prompt.
        var textSearchProvider = new TextSearchProvider(textSearchStore);
        agentThread.AIContextProviders.Add(textSearchProvider);

        // Invoke and display assistant response
        ChatMessageContent message = await agent.InvokeAsync("Where is Contoso based?", agentThread).FirstAsync();
        Console.WriteLine(message.Content);

        message = await agent.InvokeAsync("What was its expenses for 2022?", agentThread).FirstAsync();
        Console.WriteLine(message.Content);
    }

    /// <summary>
    /// Shows how to do Retrieval Augmented Generation (RAG) with citations and filtering.
    /// </summary>
    [Fact]
    public async Task RagWithCitationsAndFiltering()
    {
        using IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = new OllamaApiClient(
            uriString: TestConfiguration.Ollama.Endpoint,
            defaultModel: TestConfiguration.Ollama.EmbeddingModelId);

        // Initialize the Chroma docker container via the fixtures.
        await chromaFixture.ManualInitializeAsync();

        // Create a vector store to store our documents.
        // Note that the embedding generator provided here must be able to generate embeddings matching the
        // number of dimensions configured for the TextSearchStore below.
        using var httpClient = new HttpClient();
        using var vectorStore = new ChromaVectorStore(
            new ChromaClient(new ChromaConfigurationOptions("http://localhost:8000"), httpClient),
            new() { EmbeddingGenerator = embeddingGenerator });

        // Create a store that uses a built in schema for storing text documents
        // and provides easy upload and search capabilities.
        // The data is stored in the `FinancialDataWithCitations` collection, with the dimensions of the embedding model.
        // When searching results will be limited to those with the `group/g2` namespace.
        using var textSearchStore = new TextSearchStore<string>(vectorStore, collectionName: "FinancialDataWithCitations", vectorDimensions: TestConfiguration.Ollama.EmbeddingDimensions, new() { SearchNamespace = "group/g2" });

        // Upsert documents into the store.
        // Note that documents have different namespaces, and only the ones
        // with the `group/g2` namespace will be matched.
        await textSearchStore.UpsertDocumentsAsync(ChatCompletion_Rag_Chroma.GetSampleDocuments());

        // Create our agent.
        Kernel kernel = Kernel.CreateBuilder()
            .AddOllamaChatCompletion(modelId: TestConfiguration.Ollama.ModelId, endpoint: new Uri(TestConfiguration.Ollama.Endpoint))
            .Build();
        ChatCompletionAgent agent =
            new()
            {
                Name = AgentName,
                Instructions = AgentInstructions,
                Kernel = kernel,
            };

        // Create a thread for the agent.
        ChatHistoryAgentThread agentThread = new();

        // Create a text search provider that can automatically search the vector store
        // for documents that match the user's query and inject them into the agent's prompt.
        var textSearchProvider = new TextSearchProvider(textSearchStore);
        agentThread.AIContextProviders.Add(textSearchProvider);

        // Invoke and display assistant response
        ChatMessageContent message = await agent.InvokeAsync("What was the income of Contoso for 2023", agentThread).FirstAsync();
        Console.WriteLine(message.Content);
    }
}
