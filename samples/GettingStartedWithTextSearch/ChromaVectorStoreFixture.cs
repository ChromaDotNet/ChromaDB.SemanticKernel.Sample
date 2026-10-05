using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;

namespace GettingStartedWithTextSearch;

/// <summary>
/// Helper class for setting up and tearing down a Chroma vector store with the embeddings of Azure OpenAI, for testing purposes.
/// </summary>
public class ChromaVectorStoreFixture : ChromaVectorStoreFixtureBase<ChromaVectorStoreFixture.DataModel>
{
    /// <inheritdoc/>
    protected override IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator()
        => new AzureOpenAIClient(new Uri(TestConfiguration.AzureOpenAIEmbeddings.Endpoint), new AzureCliCredential())
            .GetEmbeddingClient(TestConfiguration.AzureOpenAIEmbeddings.DeploymentName)
            .AsIEmbeddingGenerator(1536);

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
    /// Note that each property is decorated with an attribute that specifies how the property should be treated by the vector store.
    /// This allows us to create a collection in the vector store and upsert and retrieve instances of this class without any further configuration.
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
        [VectorStoreVector(1536)]
        public string Embedding => Text;
    }
}
