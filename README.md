[![ChromaDotNet](https://raw.githubusercontent.com/ChromaDotNet/.github/main/assets/logo-64.png)](https://chromadotnet.org)

# ChromaDB.SemanticKernel.Sample

[Semantic Kernel](https://learn.microsoft.com/semantic-kernel/) samples that use [Chroma](https://www.trychroma.com/) as the vector store through [ChromaDotNet.VectorData](https://www.nuget.org/packages/ChromaDotNet.VectorData).

They follow the structure of the Semantic Kernel .NET samples: the [concept samples](https://github.com/microsoft/semantic-kernel/tree/main/dotnet/samples/Concepts) and the [text search steps](https://github.com/microsoft/semantic-kernel/tree/main/dotnet/samples/GettingStartedWithTextSearch). Each sample is an xUnit test that runs against real models when it needs one. Chroma replaces the vector store that the original sample uses. The samples that use a chat model also come with models that run locally in Ollama.

> This is a community project. It is not affiliated with or endorsed by Chroma.

Website: [chromadotnet.org](https://chromadotnet.org)

## Samples

| Sample | Description |
|---|---|
| [VectorStore_VectorSearch_MultiStore_Chroma](./samples/Concepts/Memory/VectorStore_VectorSearch_MultiStore_Chroma.cs) | Ingests a glossary into Chroma and searches it, with and without a filter. It goes through the common vector store sample code, with and without dependency injection. Adapted from [VectorStore_VectorSearch_MultiStore_Qdrant](https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/Concepts/Memory/VectorStore_VectorSearch_MultiStore_Qdrant.cs). |
| [VectorStore_DynamicDataModel_Interop_Chroma](./samples/Concepts/Memory/VectorStore_DynamicDataModel_Interop_Chroma.cs) | Writes records as dictionaries described by a record definition, then reads them back as a .NET data model. It also does the reverse. Adapted from [VectorStore_DynamicDataModel_Interop](https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/Concepts/Memory/VectorStore_DynamicDataModel_Interop.cs). |
| [VectorStore_VectorSearch_Paging_Chroma](./samples/Concepts/Memory/VectorStore_VectorSearch_Paging_Chroma.cs) | Uses `Top` and `Skip` to page through vector search results over 1,000 records. Needs no model. Adapted from [VectorStore_VectorSearch_Paging](https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/Concepts/Memory/VectorStore_VectorSearch_Paging.cs), but with the Euclidean distance. Chroma searches with an approximate index, and with the cosine distance that index can miss some of the made-up vectors in the sample. |
| [VectorStore_HybridSearch_Simple_Chroma](./samples/Concepts/Memory/VectorStore_HybridSearch_Simple_Chroma.cs) | Searches a glossary on Chroma Cloud with a vector and keywords, with and without a filter. The vector store creates the collection with a BM25 index for the full-text indexed property. Adapted from [VectorStore_HybridSearch_Simple_AzureAISearch](https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/Concepts/Memory/VectorStore_HybridSearch_Simple_AzureAISearch.cs). |
| [ChatCompletion_Rag_Chroma](./samples/Concepts/Agents/ChatCompletion_Rag_Chroma.cs) | A `ChatCompletionAgent` that answers through a `TextSearchProvider`, from documents that a `TextSearchStore` keeps in Chroma. It also covers citations and a search namespace. Adapted from [ChatCompletion_Rag](https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/Concepts/Agents/ChatCompletion_Rag.cs). |
| [ChatCompletion_Rag_Chroma_Ollama](./samples/Concepts/Agents/ChatCompletion_Rag_Chroma_Ollama.cs) | The same sample with models that run locally in Ollama. |
| [Step5_Search_With_Chroma](./samples/GettingStartedWithTextSearch/Step5_Search_With_Chroma.cs) | Searches records that have their own data model in Chroma, through `VectorStoreTextSearch`. It then either passes the results to the model in a Handlebars prompt, or gives the model the search as a function to call. Adapted from [Step4_Search_With_VectorStore](https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/GettingStartedWithTextSearch/Step4_Search_With_VectorStore.cs). |
| [Step5_Search_With_Chroma_Ollama](./samples/GettingStartedWithTextSearch/Step5_Search_With_Chroma_Ollama.cs) | The same sample with models that run locally in Ollama. |

The samples need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). They also need Docker, except the hybrid search sample, which runs on Chroma Cloud. Their fixtures start Chroma in a container and remove it at the end.

- The fixture for the concept samples starts Chroma on port 8000, as the Semantic Kernel vector store samples do with their databases. Those samples therefore also need port 8000 to be free.
- The fixture for the text search steps starts Chroma on a free port with [ChromaDotNet.Testcontainers](https://www.nuget.org/packages/ChromaDotNet.Testcontainers).

## Run the samples with Azure OpenAI

The samples read the same settings as the Semantic Kernel samples. You can set them in user secrets or in environment variables such as `AzureOpenAI__Endpoint`. The two projects read the same user secrets:

```bash
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com/" --project samples/Concepts
dotnet user-secrets set "AzureOpenAI:ChatDeploymentName" "..." --project samples/Concepts

dotnet user-secrets set "AzureOpenAIEmbeddings:Endpoint" "https://your-resource.openai.azure.com/" --project samples/Concepts
dotnet user-secrets set "AzureOpenAIEmbeddings:DeploymentName" "..." --project samples/Concepts
```

The samples ask the embedding deployment for 1,536 dimensions. The deployment must use a model that can produce them, such as `text-embedding-3-small` or `text-embedding-3-large`.

The samples sign in with the Azure CLI (`az login`). The identity must be able to use the deployments. For example, it can have the Cognitive Services OpenAI User role, or the Foundry User role on a Microsoft Foundry resource.

```bash
dotnet test samples/Concepts --filter "FullyQualifiedName~Memory.VectorStore_VectorSearch_MultiStore_Chroma." --logger "console;verbosity=detailed"
dotnet test samples/Concepts --filter "FullyQualifiedName~Memory.VectorStore_DynamicDataModel_Interop_Chroma." --logger "console;verbosity=detailed"
dotnet test samples/Concepts --filter "FullyQualifiedName~Agents.ChatCompletion_Rag_Chroma." --logger "console;verbosity=detailed"
dotnet test samples/GettingStartedWithTextSearch --filter "FullyQualifiedName~Step5_Search_With_Chroma." --logger "console;verbosity=detailed"
```

The hybrid search sample runs on Chroma Cloud, which supports hybrid search. It uses the tenant and the database from the Chroma Cloud dashboard, and it deletes its collection at the end:

```bash
dotnet user-secrets set "Chroma:Endpoint" "https://api.trychroma.com" --project samples/Concepts
dotnet user-secrets set "Chroma:ApiKey" "..." --project samples/Concepts
dotnet user-secrets set "Chroma:Tenant" "..." --project samples/Concepts
dotnet user-secrets set "Chroma:Database" "..." --project samples/Concepts

dotnet test samples/Concepts --filter "FullyQualifiedName~Memory.VectorStore_HybridSearch_Simple_Chroma." --logger "console;verbosity=detailed"
```

The paging sample needs no model:

```bash
dotnet test samples/Concepts --filter "FullyQualifiedName~Memory.VectorStore_VectorSearch_Paging_Chroma." --logger "console;verbosity=detailed"
```

## Run the samples with Ollama

[compose.yaml](./compose.yaml) runs Ollama and downloads `qwen2.5:3b` and `nomic-embed-text`. The first time, that is about 2 GB. To follow the progress, run `docker compose logs -f ollama-models`.

```bash
docker compose up -d
dotnet test samples/Concepts --filter "FullyQualifiedName~Agents.ChatCompletion_Rag_Chroma_Ollama." --logger "console;verbosity=detailed"
dotnet test samples/GettingStartedWithTextSearch --filter "FullyQualifiedName~Step5_Search_With_Chroma_Ollama." --logger "console;verbosity=detailed"
```

The defaults match the compose file. To change them, set:

- `Ollama:Endpoint`
- `Ollama:ModelId`
- `Ollama:EmbeddingModelId`
- `Ollama:EmbeddingDimensions`

The data model of `Step5_Search_With_Chroma_Ollama` has the 768 dimensions of `nomic-embed-text` in its attribute. You can find it in [ChromaVectorStoreOllamaFixture.cs](./samples/GettingStartedWithTextSearch/ChromaVectorStoreOllamaFixture.cs).

A larger chat model than `qwen2.5:3b` gives better answers. `qwen2.5:3b` answers from the documents, but it can add made-up links when they have no source.

## Tests

```bash
dotnet test test/ChromaDB.SemanticKernel.Sample.Tests
```

GitHub Actions runs them on every push to `main` and on every pull request to `main`.

The tests run the scenario of each sample with the same configuration, except the hybrid search, which needs Chroma Cloud. They run against Chroma in a container that ChromaDotNet.Testcontainers starts on a free port. They need Docker but no model. The embeddings come from word hashes, and the chat model only records what it receives.

- Vector search: each search finds the glossary entry it asks about. The category filter leaves out the entry in the other category, Connectors. Both checks pass with and without dependency injection.
- Data models: a record written as a dictionary reads back as the .NET data model, and the other way round, including the vector.
- Paging: the pages go through each of the 1,000 records once, ordered by distance.
- RAG: the document that answers each question reaches the model. With a search namespace, only the documents in that namespace reach it, along with their source.
- Text search: the search returns the records with their key, text and link. The Handlebars prompt receives the search results. With function calling, the search results reach the model.

The following files come from the [Semantic Kernel repository](https://github.com/microsoft/semantic-kernel) (MIT):

- the files in [samples/InternalUtilities](./samples/InternalUtilities/)
- the common vector store sample code
- the container helper

They are reduced to what these samples use and keep their copyright notice. The samples and their fixtures are adaptations of the Semantic Kernel samples linked in the table above. They are under the same license and carry the same copyright notice.
