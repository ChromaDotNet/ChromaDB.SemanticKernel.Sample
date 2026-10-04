# ChromaDB.SemanticKernel.Sample

A [Semantic Kernel](https://learn.microsoft.com/semantic-kernel/) assistant that uses [Chroma](https://www.trychroma.com/) as its vector store, through [ChromaDotNet.VectorData](https://www.nuget.org/packages/ChromaDotNet.VectorData).

> This is a community project. It is not affiliated with or endorsed by Chroma.

The assistant answers the customers of Lakeside Bike Rental, a shop that exists only in this sample, so the model can answer only from what it finds in Chroma:

- **Retrieval store.** The documents of the shop are stored in a Chroma collection by a [`TextSearchStore`](https://learn.microsoft.com/semantic-kernel/concepts/text-search/), the store of Semantic Kernel for retrieval, which defines the schema of the collection.
- **Search inside the prompt.** The store is added to the kernel as a plugin, and a [Handlebars prompt template](https://learn.microsoft.com/semantic-kernel/concepts/prompts/handlebars-prompt-templates) calls it: the passages found in Chroma are written into the prompt before it reaches the model, so the model does not need function calling.
- **Namespaces.** The customer policies and the staff notes share the collection in two namespaces, and the search sees only the customer documents.

The models run locally with [Ollama](https://ollama.com/): `qwen2.5:3b` for the chat and `all-minilm` for the embeddings. No API key is needed.

## Run it

You need Docker and the .NET 10 SDK.

```bash
docker compose up -d
dotnet run --project src/RentalAssistant
```

The first `docker compose up` downloads the two models, about 2 GB; `docker compose logs -f ollama-models` shows the progress. The chat model answers at temperature 0 and the images are pinned, so on the same machine every run gives these same answers.

An actual run:

```text
> How much does an e-bike cost for a whole day?
An e-bike costs 30 euros for a whole day at Lakeside Bike Rental.
> Can I return the bike at another shop?
Yes, you can return the bike at any of our three shops: Harbour, Old Town, or Lakeside Station.
> Is there a discount code I can use?
No, there is no specific discount code mentioned in the information provided. The passage only mentions that students and groups of five or more get a 10% discount by showing their student card.
```

The staff notes include a discount code, but they are in another namespace: the search in Chroma never returns them, so the model never sees the code.

## Settings

| Variable | Default |
|---|---|
| `CHROMA_URI` | `http://localhost:8000` |
| `OLLAMA_URI` | `http://localhost:11434` |
| `CHAT_MODEL` | `qwen2.5:3b` |
| `EMBEDDING_MODEL` | `all-minilm` |
| `EMBEDDING_DIMENSIONS` | `384`, the dimensions of `all-minilm` |

## Tests

```bash
dotnet test
```

GitHub Actions runs them on every push.

The tests start Chroma in a container with [ChromaDotNet.Testcontainers](https://www.nuget.org/packages/ChromaDotNet.Testcontainers), so they need Docker, but no model: the embeddings come from word hashes and the chat model only records the prompt it receives. They check that the documents are stored in Chroma, that storing them again replaces them, that the passage that answers each question reaches the prompt, that the staff notes never do, and that the prompt asks the model for temperature 0.
