namespace GettingStartedWithTextSearch;

[CollectionDefinition("ChromaVectorStoreCollection")]
public class ChromaVectorStoreCollectionFixture : ICollectionFixture<ChromaVectorStoreFixture>
{
}

[CollectionDefinition("ChromaVectorStoreOllamaCollection")]
public class ChromaVectorStoreOllamaCollectionFixture : ICollectionFixture<ChromaVectorStoreOllamaFixture>
{
}
