using ChromaDB.Client;
using ChromaDB.VectorData;
using Microsoft.Extensions.AI;
using OllamaSharp;

var chromaUri = Setting("CHROMA_URI", "http://localhost:8000");
var ollamaUri = Setting("OLLAMA_URI", "http://localhost:11434");
var chatModel = Setting("CHAT_MODEL", "qwen2.5:3b");
var embeddingModel = Setting("EMBEDDING_MODEL", "all-minilm");
var embeddingDimensions = int.Parse(Setting("EMBEDDING_DIMENSIONS", "384"));

using IChatClient chatClient = new OllamaApiClient(ollamaUri, chatModel);
using IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = new OllamaApiClient(ollamaUri, embeddingModel);
using var httpClient = new HttpClient();
using var vectorStore = new ChromaVectorStore(
    new ChromaClient(new ChromaConfigurationOptions(chromaUri), httpClient),
    new ChromaVectorStoreOptions { EmbeddingGenerator = embeddingGenerator });

using var assistant = await RentalAssistant.RentalAssistant.CreateAsync(chatClient, vectorStore, embeddingDimensions);

foreach (var question in new[]
{
    "How much does an e-bike cost for a whole day?",
    "Can I return the bike at another shop?",
    "Is there a discount code I can use?",
})
{
    Console.WriteLine($"> {question}");
    Console.WriteLine(await assistant.AskAsync(question));
}

static string Setting(string name, string defaultValue)
    => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : defaultValue;
