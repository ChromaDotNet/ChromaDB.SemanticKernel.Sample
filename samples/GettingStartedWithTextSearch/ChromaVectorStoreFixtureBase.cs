// Copyright (c) Microsoft. All rights reserved.

using System.Reflection;
using ChromaDB.Client;
using ChromaDB.VectorData;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.VectorData;
using Testcontainers.Chroma;

namespace GettingStartedWithTextSearch;

/// <summary>
/// Helper class for setting up and tearing down a <see cref="ChromaVectorStore"/> for testing purposes:
/// it starts Chroma in a container, stores the sample records in a collection, and removes the container at the end.
/// </summary>
/// <typeparam name="TRecord">The type of the records stored in the collection.</typeparam>
public abstract class ChromaVectorStoreFixtureBase<TRecord> : IAsyncLifetime
    where TRecord : class
{
    private readonly ChromaContainer _chromaContainer = new ChromaBuilder("chromadb/chroma:1.5.9").Build();
    private readonly HttpClient _httpClient = new();

    /// <summary>
    /// Gets the embedding generator used for creating vector embeddings.
    /// </summary>
    public IEmbeddingGenerator<string, Embedding<float>> EmbeddingGenerator { get; }

    /// <summary>
    /// Gets the Chroma vector store instance.
    /// </summary>
    public ChromaVectorStore ChromaVectorStore { get; private set; }

    /// <summary>
    /// Gets the vector store record collection for data models.
    /// </summary>
    public VectorStoreCollection<Guid, TRecord> VectorStoreRecordCollection { get; private set; }

    /// <summary>
    /// Gets the name of the collection used for storing records.
    /// </summary>
    public string CollectionName => "records";

    /// <summary>
    /// Initializes a new instance of the <see cref="ChromaVectorStoreFixtureBase{TRecord}"/> class.
    /// </summary>
    protected ChromaVectorStoreFixtureBase()
    {
        IConfigurationRoot configRoot = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Development.json", true)
            .AddEnvironmentVariables()
            .AddUserSecrets(Assembly.GetExecutingAssembly())
            .Build();
        TestConfiguration.Initialize(configRoot);

        // Create an embedding generation service.
        this.EmbeddingGenerator = this.CreateEmbeddingGenerator();
    }

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        // Start Chroma in a container and create a Chroma vector store that generates the embeddings of the records.
        await this._chromaContainer.StartAsync();
        this.ChromaVectorStore = new ChromaVectorStore(
            new ChromaClient(new ChromaConfigurationOptions(this._chromaContainer.GetConnectionString()), this._httpClient),
            ownsClient: true,
            new() { EmbeddingGenerator = this.EmbeddingGenerator });

        this.VectorStoreRecordCollection = await this.InitializeRecordCollectionAsync();
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        this.ChromaVectorStore?.Dispose();
        this._httpClient.Dispose();
        await this._chromaContainer.DisposeAsync();
    }

    /// <summary>
    /// Creates the embedding generator of the vector store.
    /// </summary>
    protected abstract IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator();

    /// <summary>
    /// Creates a record with a unique key for a line of text.
    /// </summary>
    /// <param name="index">The index of the line.</param>
    /// <param name="text">The line of text.</param>
    protected abstract TRecord CreateRecord(int index, string text);

    #region private
    /// <summary>
    /// Initialize a <see cref="VectorStoreCollection{TKey, TRecord}"/> with a list of strings.
    /// </summary>
    private async Task<VectorStoreCollection<Guid, TRecord>> InitializeRecordCollectionAsync()
    {
        // Get and create collection if it doesn't exist.
        var collection = this.ChromaVectorStore.GetCollection<Guid, TRecord>(this.CollectionName);
        await collection.EnsureCollectionExistsAsync().ConfigureAwait(false);

        // Create a record for each line and insert the records into the collection.
        // The vector store generates the embeddings of the records as it inserts them.
        await collection.UpsertAsync(Lines.Select((text, index) => this.CreateRecord(index, text))).ConfigureAwait(false);

        return collection;
    }

    private static readonly string[] Lines =
    [
        "Semantic Kernel is a lightweight, open-source development kit that lets you easily build AI agents and integrate the latest AI models into your C#, Python, or Java codebase. It serves as an efficient middleware that enables rapid delivery of enterprise-grade solutions.",
        "Semantic Kernel is a new AI SDK, and a simple and yet powerful programming model that lets you add large language capabilities to your app in just a matter of minutes. It uses natural language prompting to create and execute semantic kernel AI tasks across multiple languages and platforms.",
        "In this guide, you learned how to quickly get started with Semantic Kernel by building a simple AI agent that can interact with an AI service and run your code. To see more examples and learn how to build more complex AI agents, check out our in-depth samples.",
        "The Semantic Kernel extension for Visual Studio Code makes it easy to design and test semantic functions.The extension provides an interface for designing semantic functions and allows you to test them with the push of a button with your existing models and data.",
        "The kernel is the central component of Semantic Kernel.At its simplest, the kernel is a Dependency Injection container that manages all of the services and plugins necessary to run your AI application.",
        "Semantic Kernel (SK) is a lightweight SDK that lets you mix conventional programming languages, like C# and Python, with the latest in Large Language Model (LLM) AI “prompts” with prompt templating, chaining, and planning capabilities.",
        "Semantic Kernel is a lightweight, open-source development kit that lets you easily build AI agents and integrate the latest AI models into your C#, Python, or Java codebase. It serves as an efficient middleware that enables rapid delivery of enterprise-grade solutions. Enterprise ready.",
        "With Semantic Kernel, you can easily build agents that can call your existing code.This power lets you automate your business processes with models from OpenAI, Azure OpenAI, Hugging Face, and more! We often get asked though, “How do I architect my solution?” and “How does it actually work?”"
    ];
    #endregion
}
