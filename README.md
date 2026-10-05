# ChromaDB.SemanticKernel.Sample

[Semantic Kernel](https://learn.microsoft.com/semantic-kernel/) samples that use [Chroma](https://www.trychroma.com/) as the vector store, through [ChromaDotNet.VectorData](https://www.nuget.org/packages/ChromaDotNet.VectorData). They follow the form of the [.NET concept samples of Semantic Kernel](https://github.com/microsoft/semantic-kernel/tree/main/dotnet/samples/Concepts): each sample is an xUnit test that runs against real models, with Chroma in place of the vector store of the original sample. The sample that uses a chat model also comes with models that run locally in Ollama.

> This is a community project. It is not affiliated with or endorsed by Chroma.

## Samples

| Sample | Description |
|---|---|
| [VectorStore_VectorSearch_MultiStore_Chroma](./samples/Concepts/Memory/VectorStore_VectorSearch_MultiStore_Chroma.cs) | Ingests a glossary into Chroma and searches it, also with a filter, through the common code of the vector store samples, with and without dependency injection. In the form of [VectorStore_VectorSearch_MultiStore_Qdrant](https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/Concepts/Memory/VectorStore_VectorSearch_MultiStore_Qdrant.cs). |
| [ChatCompletion_Rag_Chroma](./samples/Concepts/Agents/ChatCompletion_Rag_Chroma.cs) | A `ChatCompletionAgent` that answers from documents stored in Chroma by a `TextSearchStore`, through a `TextSearchProvider`, also with citations and a search namespace. In the form of [ChatCompletion_Rag](https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/Concepts/Agents/ChatCompletion_Rag.cs). |
| [ChatCompletion_Rag_Chroma_Ollama](./samples/Concepts/Agents/ChatCompletion_Rag_Chroma_Ollama.cs) | The same sample with models that run locally in Ollama. |

The fixture of the samples starts Chroma in a container on port 8000 and removes it at the end, so the samples need Docker and a free port 8000.

## Run the samples with Azure OpenAI

The samples read the settings of the Semantic Kernel samples, from user secrets or from environment variables such as `AzureOpenAI__Endpoint`:

```bash
cd samples/Concepts

dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:ChatDeploymentName" "..."

dotnet user-secrets set "AzureOpenAIEmbeddings:Endpoint" "https://your-resource.openai.azure.com/"
dotnet user-secrets set "AzureOpenAIEmbeddings:DeploymentName" "..."
```

The embedding deployment must produce embeddings with 1,536 dimensions, like `text-embedding-3-small` or `text-embedding-3-large`. The samples sign in with the Azure CLI (`az login`), with an identity that can use the deployments, like one with the Cognitive Services OpenAI User role, or the Foundry User role on a Microsoft Foundry resource.

```bash
dotnet test --filter "FullyQualifiedName~Memory.VectorStore_VectorSearch_MultiStore_Chroma." --logger "console;verbosity=detailed"
dotnet test --filter "FullyQualifiedName~Agents.ChatCompletion_Rag_Chroma." --logger "console;verbosity=detailed"
```

## Run the samples with Ollama

[compose.yaml](./compose.yaml) runs Ollama and downloads `qwen2.5:3b` and `nomic-embed-text`, about 2 GB the first time; `docker compose logs -f ollama-models` shows the progress.

```bash
docker compose up -d
dotnet test samples/Concepts --filter "FullyQualifiedName~Agents.ChatCompletion_Rag_Chroma_Ollama." --logger "console;verbosity=detailed"
```

The defaults match the compose file. To change them, set `Ollama:Endpoint`, `Ollama:ModelId`, `Ollama:EmbeddingModelId` and `Ollama:EmbeddingDimensions`. A larger chat model than `qwen2.5:3b` gives better answers: it answers from the documents, but it can add made-up links when they have no source.

## Tests

```bash
dotnet test test/ChromaDB.SemanticKernel.Sample.Tests
```

GitHub Actions runs them on every push.

The tests run the scenario of each sample, with the same configuration, against Chroma in a container started on a free port with [ChromaDotNet.Testcontainers](https://www.nuget.org/packages/ChromaDotNet.Testcontainers). They need Docker, but no model: the embeddings come from word hashes and the chat model only records what the agent sends to it.

- Vector search: each search finds the glossary entry it asks about, and the filter on the category leaves out the third entry, with and without dependency injection.
- RAG: the document that answers each question reaches the model, and with a search namespace only the documents of the namespace reach it, with their source.

The files in [samples/InternalUtilities](./samples/InternalUtilities/), the common code of the vector store samples and the container helper come from the [Semantic Kernel repository](https://github.com/microsoft/semantic-kernel) (MIT), reduced to what these samples use, and keep their copyright notice.
