using ChromaDB.Client;
using ChromaDB.VectorData;
using Memory;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace ChromaDB.SemanticKernel.Sample.Tests;

/// <summary>
/// Runs <see cref="VectorStore_VectorSearch_MultiStore_Chroma"/> with the embeddings of <see cref="WordEmbeddingGenerator"/>.
/// </summary>
public sealed class MultiStoreTests(ChromaFixture fixture, ITestOutputHelper testOutput) : IClassFixture<ChromaFixture>
{
    // The dimensions of the vector of the glossary entries.
    private const int EmbeddingDimensions = 1536;

    [Fact]
    public async Task With_DI_each_search_finds_its_glossary_entry()
    {
        var output = new RecordingOutput(testOutput);
        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(new WordEmbeddingGenerator(EmbeddingDimensions));
        kernelBuilder.Services.AddChromaVectorStore(fixture.Endpoint);
        kernelBuilder.Services.AddSingleton<ITestOutputHelper>(output);
        kernelBuilder.Services.AddTransient<VectorStore_VectorSearch_MultiStore_Common>();
        var kernel = kernelBuilder.Build();

        var processor = kernel.GetRequiredService<VectorStore_VectorSearch_MultiStore_Common>();
        await processor.IngestDataAndSearchAsync("skglossaryWithDI", () => Guid.NewGuid());

        AssertResults(output);
    }

    [Fact]
    public async Task Without_DI_each_search_finds_its_glossary_entry()
    {
        var output = new RecordingOutput(testOutput);
        using var httpClient = new HttpClient();
        using var vectorStore = new ChromaVectorStore(new ChromaClient(new ChromaConfigurationOptions(fixture.Endpoint), httpClient));

        var processor = new VectorStore_VectorSearch_MultiStore_Common(vectorStore, new WordEmbeddingGenerator(EmbeddingDimensions), output);
        await processor.IngestDataAndSearchAsync("skglossaryWithoutDI", () => Guid.NewGuid());

        AssertResults(output);
    }

    // The two searches find the entry they ask about, and the filter on the category leaves out the entry of the other category, Connectors.
    private static void AssertResults(RecordingOutput output)
        => Assert.Collection(
            output.Lines.Where(line => (line.StartsWith("Result") && !line.Contains("Score")) || line.StartsWith("Number of results")),
            line => Assert.StartsWith("Result: Application Programming Interface.", line),
            line => Assert.StartsWith("Result: Retrieval Augmented Generation", line),
            line => Assert.Equal("Number of results: 2", line),
            line => Assert.StartsWith("Result 1: Retrieval Augmented Generation", line),
            line => Assert.StartsWith("Result 2: Application Programming Interface.", line));
}
