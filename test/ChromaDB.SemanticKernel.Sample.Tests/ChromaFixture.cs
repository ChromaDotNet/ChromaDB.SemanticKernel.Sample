using Testcontainers.Chroma;

namespace ChromaDB.SemanticKernel.Sample.Tests;

/// <summary>
/// Starts Chroma in a container for the tests, on a free port, so that it does not need port 8000 of the samples.
/// </summary>
public sealed class ChromaFixture : IAsyncLifetime
{
    private readonly ChromaContainer _container = new ChromaBuilder("chromadb/chroma:1.5.9").Build();

    public string Endpoint => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
