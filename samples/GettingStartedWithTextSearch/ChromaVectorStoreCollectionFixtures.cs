// Copyright (c) Microsoft. All rights reserved.

namespace GettingStartedWithTextSearch;

[CollectionDefinition("ChromaVectorStoreCollection")]
public class ChromaVectorStoreCollectionFixture : ICollectionFixture<ChromaVectorStoreFixture>
{
}

[CollectionDefinition("ChromaVectorStoreOllamaCollection")]
public class ChromaVectorStoreOllamaCollectionFixture : ICollectionFixture<ChromaVectorStoreOllamaFixture>
{
}
