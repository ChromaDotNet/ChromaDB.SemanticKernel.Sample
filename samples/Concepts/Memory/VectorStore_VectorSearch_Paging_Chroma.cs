// Copyright (c) Microsoft. All rights reserved.

using ChromaDB.Client;
using ChromaDB.VectorData;
using Memory.VectorStoreFixtures;
using Microsoft.Extensions.VectorData;

namespace Memory;

/// <summary>
/// An example showing how to do paging when there are many records in the database and you want to page through these page by page.
///
/// The example shows the following steps:
/// 1. Create a Chroma Vector Store.
/// 2. Generate and add some test data entries.
/// 3. Read the data back using vector search by paging through the results page by page.
///
/// To run this sample, you need a local instance of Docker running, since the associated fixture will try and start a Chroma container in the local docker instance.
/// </summary>
public class VectorStore_VectorSearch_Paging_Chroma(ITestOutputHelper output, VectorStoreChromaContainerFixture chromaFixture) : BaseTest(output), IClassFixture<VectorStoreChromaContainerFixture>
{
    [Fact]
    public async Task VectorSearchWithPagingAsync()
    {
        // Initiate the docker container and construct the Chroma vector store.
        await chromaFixture.ManualInitializeAsync();
        using var httpClient = new HttpClient();
        using var vectorStore = new ChromaVectorStore(new ChromaClient(new ChromaConfigurationOptions("http://localhost:8000"), httpClient));

        // Get and create collection if it doesn't exist.
        // Chroma supports string and Guid keys.
        var collection = vectorStore.GetCollection<string, TextSnippet>("skglossary");
        await collection.EnsureCollectionExistsAsync();

        // Create some test data entries.
        // We are not generating real embeddings here, just some random numbers
        // to keep the example simple.
        for (int i = 0; i < 1000; i++)
        {
            var text = $"This is a test text snippet {i}";
            var embedding = new ReadOnlyMemory<float>([i, i + 1, i + 2, i + 3]);
            var textSnippet = new TextSnippet { Key = i.ToString(), Text = text, TextEmbedding = embedding };
            await collection.UpsertAsync(textSnippet);
        }

        // Create a vector to search with.
        // We are not generating a real embedding here, just some random numbers
        // to keep the example simple.
        var searchVector = new ReadOnlyMemory<float>([0, 1, 2, 3]);

        // Loop until there are no more results.
        var page = 0;
        var moreResults = true;
        while (moreResults)
        {
            // Get the next page of results by asking for 10 results, and using 'Skip' to skip the results from the previous pages.
            var currentPageResults = collection.SearchAsync(
                searchVector,
                top: 10,
                new()
                {
                    Skip = page * 10
                });

            // Print the results.
            var pageCount = 0;
            await foreach (var result in currentPageResults)
            {
                Console.WriteLine($"Key: {result.Record.Key}, Text: {result.Record.Text}");
                pageCount++;
            }

            // Stop when we got back less than the requested number of results.
            moreResults = pageCount == 10;
            page++;
        }
    }

    /// <summary>
    /// Sample model class that can store some text and its embedding.
    /// </summary>
    /// <remarks>
    /// Note that each property is decorated with an attribute that specifies how the property should be treated by the vector store.
    /// This allows us to create a collection in the vector store and upsert and retrieve instances of this class without any further configuration.
    /// </remarks>
    private sealed class TextSnippet
    {
        [VectorStoreKey]
        public string Key { get; set; }

        [VectorStoreData]
        public string Text { get; set; }

        // Chroma searches with an approximate index (HNSW). With the default cosine distance, these made-up
        // vectors point in nearly the same direction as their number grows, and the index can miss some of them;
        // with the Euclidean distance they stay far apart, and the pages follow the order of the keys.
        [VectorStoreVector(4, DistanceFunction = DistanceFunction.EuclideanDistance)]
        public ReadOnlyMemory<float> TextEmbedding { get; set; }
    }
}
