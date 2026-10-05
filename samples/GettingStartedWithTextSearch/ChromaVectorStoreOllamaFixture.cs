using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;
using OllamaSharp;

namespace GettingStartedWithTextSearch;

/// <summary>
/// Helper class for setting up and tearing down a Chroma vector store with the embeddings of a model that runs locally in Ollama, for testing purposes.
/// </summary>
public class ChromaVectorStoreOllamaFixture : ChromaVectorStoreFixtureBase<ChromaVectorStoreOllamaFixture.DataModel>
{
    /// <inheritdoc/>
    protected override IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator()
        => new OllamaApiClient(
            uriString: TestConfiguration.Ollama.Endpoint,
            defaultModel: TestConfiguration.Ollama.EmbeddingModelId);

    /// <inheritdoc/>
    protected override DataModel CreateRecord(int index, string text)
    {
        var guid = Guid.NewGuid();
        return new()
        {
            Key = guid,
            Text = text,
            Link = $"guid://{guid}",
            Tag = index % 2 == 0 ? "Even" : "Odd",
        };
    }

    /// <summary>
    /// Sample model class that represents a record entry.
    /// </summary>
    /// <remarks>
    /// The same as <see cref="ChromaVectorStoreFixture.DataModel"/>, with the 768 dimensions of the embeddings of
    /// <c>nomic-embed-text</c>, the default embedding model of <see cref="TestConfiguration.Ollama"/>.
    /// </remarks>
    public sealed class DataModel
    {
        /// <summary>
        /// Gets or sets the unique identifier for this record.
        /// </summary>
        [VectorStoreKey]
        [TextSearchResultName]
        public Guid Key { get; init; }

        /// <summary>
        /// Gets or sets the text content of this record.
        /// </summary>
        [VectorStoreData]
        [TextSearchResultValue]
        public string Text { get; init; }

        /// <summary>
        /// Gets or sets the link associated with this record.
        /// </summary>
        [VectorStoreData]
        [TextSearchResultLink]
        public string Link { get; init; }

        /// <summary>
        /// Gets or sets the tag for categorizing this record.
        /// </summary>
        [VectorStoreData(IsIndexed = true)]
        public required string Tag { get; init; }

        /// <summary>
        /// Gets the embedding representation of the text content.
        /// </summary>
        [VectorStoreVector(768)]
        public string Embedding => Text;
    }
}
