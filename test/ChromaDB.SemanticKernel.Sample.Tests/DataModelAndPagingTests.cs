using ChromaDB.Client;
using ChromaDB.VectorData;
using Memory;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace ChromaDB.SemanticKernel.Sample.Tests;

/// <summary>
/// Runs <see cref="VectorStore_DynamicDataModel_Interop_Chroma"/> with the embeddings of <see cref="WordEmbeddingGenerator"/>,
/// and <see cref="VectorStore_VectorSearch_Paging_Chroma"/>, which needs no model.
/// </summary>
public sealed class DataModelAndPagingTests(ChromaFixture fixture) : IClassFixture<ChromaFixture>
{
    private const string ApiDefinition = "Application Programming Interface. A set of rules and specifications that allow software components to communicate and exchange data.";

    private static readonly VectorStoreCollectionDefinition s_definition = new()
    {
        Properties =
        [
            new VectorStoreKeyProperty("Key", typeof(string)),
            new VectorStoreDataProperty("Term", typeof(string)),
            new VectorStoreDataProperty("Definition", typeof(string)),
            new VectorStoreVectorProperty("DefinitionEmbedding", typeof(ReadOnlyMemory<float>), 1536)
        ]
    };

    private readonly WordEmbeddingGenerator _embeddingGenerator = new(1536);

    [Fact]
    public async Task A_record_upserted_as_a_dictionary_reads_back_as_the_custom_data_model()
    {
        using var httpClient = new HttpClient();
        using var vectorStore = CreateVectorStore(httpClient);
        var dynamicDataModelCollection = vectorStore.GetDynamicCollection("skglossary-dynamic", s_definition);
        await dynamicDataModelCollection.EnsureCollectionExistsAsync();
        var embedding = await _embeddingGenerator.GenerateVectorAsync(ApiDefinition);
        await dynamicDataModelCollection.UpsertAsync(new Dictionary<string, object?>
        {
            ["Key"] = "1",
            ["Term"] = "API",
            ["Definition"] = ApiDefinition,
            ["DefinitionEmbedding"] = embedding,
        });

        var customDataModelCollection = vectorStore.GetCollection<string, Glossary>("skglossary-dynamic");
        var record = await customDataModelCollection.GetAsync("1", new() { IncludeVectors = true });

        Assert.NotNull(record);
        Assert.Equal("1", record.Key);
        Assert.Equal("API", record.Term);
        Assert.Equal(ApiDefinition, record.Definition);
        Assert.Equal(embedding.ToArray(), record.DefinitionEmbedding.ToArray());
    }

    [Fact]
    public async Task A_record_upserted_as_the_custom_data_model_reads_back_as_a_dictionary()
    {
        using var httpClient = new HttpClient();
        using var vectorStore = CreateVectorStore(httpClient);
        var customDataModelCollection = vectorStore.GetCollection<string, Glossary>("skglossary-custom");
        await customDataModelCollection.EnsureCollectionExistsAsync();
        var embedding = await _embeddingGenerator.GenerateVectorAsync(ApiDefinition);
        await customDataModelCollection.UpsertAsync(new Glossary { Key = "1", Term = "API", Definition = ApiDefinition, DefinitionEmbedding = embedding });

        var dynamicDataModelCollection = vectorStore.GetDynamicCollection("skglossary-custom", s_definition);
        var record = await dynamicDataModelCollection.GetAsync("1", new() { IncludeVectors = true });

        Assert.NotNull(record);
        Assert.Equal("1", record["Key"]);
        Assert.Equal("API", record["Term"]);
        Assert.Equal(ApiDefinition, record["Definition"]);
        Assert.Equal(embedding.ToArray(), Assert.IsType<ReadOnlyMemory<float>>(record["DefinitionEmbedding"]).ToArray());
    }

    [Fact]
    public async Task The_pages_go_through_every_record_once_in_the_order_of_the_distance()
    {
        using var httpClient = new HttpClient();
        using var vectorStore = CreateVectorStore(httpClient);
        var collection = vectorStore.GetCollection<string, TextSnippet>("skglossary-paging");
        await collection.EnsureCollectionExistsAsync();
        for (int i = 0; i < 1000; i++)
        {
            await collection.UpsertAsync(new TextSnippet { Key = i.ToString(), Text = $"This is a test text snippet {i}", TextEmbedding = new ReadOnlyMemory<float>([i, i + 1, i + 2, i + 3]) });
        }

        var keys = new List<string>();
        var page = 0;
        var moreResults = true;
        while (moreResults)
        {
            var pageCount = 0;
            await foreach (var result in collection.SearchAsync(new ReadOnlyMemory<float>([0, 1, 2, 3]), top: 10, new() { Skip = page * 10 }))
            {
                keys.Add(result.Record.Key);
                pageCount++;
            }

            moreResults = pageCount == 10;
            page++;
        }

        Assert.Equal(Enumerable.Range(0, 1000).Select(i => i.ToString()), keys);
    }

    private ChromaVectorStore CreateVectorStore(HttpClient httpClient)
        => new(new ChromaClient(new ChromaConfigurationOptions(fixture.Endpoint), httpClient));

    private sealed class Glossary
    {
        [VectorStoreKey]
        public string Key { get; set; } = "";

        [VectorStoreData]
        public string Term { get; set; } = "";

        [VectorStoreData]
        public string Definition { get; set; } = "";

        [VectorStoreVector(1536)]
        public ReadOnlyMemory<float> DefinitionEmbedding { get; set; }
    }

    private sealed class TextSnippet
    {
        [VectorStoreKey]
        public string Key { get; set; } = "";

        [VectorStoreData]
        public string Text { get; set; } = "";

        [VectorStoreVector(4, DistanceFunction = DistanceFunction.EuclideanDistance)]
        public ReadOnlyMemory<float> TextEmbedding { get; set; }
    }
}
