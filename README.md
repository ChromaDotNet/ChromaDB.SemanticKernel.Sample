[![ChromaDotNet](https://raw.githubusercontent.com/ChromaDotNet/.github/main/assets/logo-64.png)](https://chromadotnet.org)

# ChromaDB.SemanticKernel.Sample

[Semantic Kernel](https://learn.microsoft.com/semantic-kernel/) samples that use [Chroma](https://www.trychroma.com/) as the vector store, through [ChromaDotNet.VectorData](https://www.nuget.org/packages/ChromaDotNet.VectorData). They follow the form of the .NET samples of Semantic Kernel, the [concept samples](https://github.com/microsoft/semantic-kernel/tree/main/dotnet/samples/Concepts) and the [steps of text search](https://github.com/microsoft/semantic-kernel/tree/main/dotnet/samples/GettingStartedWithTextSearch): each sample is an xUnit test that runs against real models when it needs one, with Chroma in place of the vector store of the original sample. The samples that use a chat model also come with models that run locally in Ollama.

> This is a community project. It is not affiliated with or endorsed by Chroma.

Website: [chromadotnet.org](https://chromadotnet.org)

## Samples

| Sample | Description |
|---|---|
| [VectorStore_VectorSearch_MultiStore_Chroma](./samples/Concepts/Memory/VectorStore_VectorSearch_MultiStore_Chroma.cs) | Ingests a glossary into Chroma and searches it, also with a filter, through the common code of the vector store samples, with and without dependency injection. In the form of [VectorStore_VectorSearch_MultiStore_Qdrant](https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/Concepts/Memory/VectorStore_VectorSearch_MultiStore_Qdrant.cs). |
| [VectorStore_DynamicDataModel_Interop_Chroma](./samples/Concepts/Memory/VectorStore_DynamicDataModel_Interop_Chroma.cs) | Writes records as dictionaries described by a record definition and reads them back as a .NET data model, and the other way round. In the form of [VectorStore_DynamicDataModel_Interop](https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/Concepts/Memory/VectorStore_DynamicDataModel_Interop.cs). |
| [VectorStore_VectorSearch_Paging_Chroma](./samples/Concepts/Memory/VectorStore_VectorSearch_Paging_Chroma.cs) | Pages through the results of a vector search over 1,000 records with `Top` and `Skip`, without a model. In the form of [VectorStore_VectorSearch_Paging](https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/Concepts/Memory/VectorStore_VectorSearch_Paging.cs), with the Euclidean distance: Chroma searches with an approximate index, which with the cosine distance can miss some of the made-up vectors of the sample. |
| [VectorStore_HybridSearch_Simple_Chroma](./samples/Concepts/Memory/VectorStore_HybridSearch_Simple_Chroma.cs) | Searches a glossary with a vector and keywords, also with a filter, on Chroma Cloud, in a collection that the vector store creates with a BM25 index for the full-text indexed property. In the form of [VectorStore_HybridSearch_Simple_AzureAISearch](https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/Concepts/Memory/VectorStore_HybridSearch_Simple_AzureAISearch.cs). |
| [ChatCompletion_Rag_Chroma](./samples/Concepts/Agents/ChatCompletion_Rag_Chroma.cs) | A `ChatCompletionAgent` that answers from documents stored in Chroma by a `TextSearchStore`, through a `TextSearchProvider`, also with citations and a search namespace. In the form of [ChatCompletion_Rag](https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/Concepts/Agents/ChatCompletion_Rag.cs). |
| [ChatCompletion_Rag_Chroma_Ollama](./samples/Concepts/Agents/ChatCompletion_Rag_Chroma_Ollama.cs) | The same sample with models that run locally in Ollama. |
| [Step5_Search_With_Chroma](./samples/GettingStartedWithTextSearch/Step5_Search_With_Chroma.cs) | Searches records with a data model of their own in Chroma through `VectorStoreTextSearch`, then gives the results to the model in a Handlebars prompt, or gives the search to the model as a function to call. In the form of [Step4_Search_With_VectorStore](https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/GettingStartedWithTextSearch/Step4_Search_With_VectorStore.cs). |
| [Step5_Search_With_Chroma_Ollama](./samples/GettingStartedWithTextSearch/Step5_Search_With_Chroma_Ollama.cs) | The same sample with models that run locally in Ollama. |

The samples need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), and Docker, except the hybrid search sample, which runs on Chroma Cloud: their fixtures start Chroma in a container and remove it at the end. The fixture of the concept samples starts it on port 8000, as the vector store samples of Semantic Kernel do with their databases, so those samples also need a free port 8000. The fixture of the text search steps starts it on a free port with [ChromaDotNet.Testcontainers](https://www.nuget.org/packages/ChromaDotNet.Testcontainers).

## Run the samples with Azure OpenAI

The samples read the settings of the Semantic Kernel samples, from user secrets or from environment variables such as `AzureOpenAI__Endpoint`. The two projects read the same user secrets:

```bash
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com/" --project samples/Concepts
dotnet user-secrets set "AzureOpenAI:ChatDeploymentName" "..." --project samples/Concepts

dotnet user-secrets set "AzureOpenAIEmbeddings:Endpoint" "https://your-resource.openai.azure.com/" --project samples/Concepts
dotnet user-secrets set "AzureOpenAIEmbeddings:DeploymentName" "..." --project samples/Concepts
```

The samples ask the embedding deployment for 1,536 dimensions, so it must be a model that can produce them, like `text-embedding-3-small` or `text-embedding-3-large`. The samples sign in with the Azure CLI (`az login`), with an identity that can use the deployments, like one with the Cognitive Services OpenAI User role, or the Foundry User role on a Microsoft Foundry resource.

```bash
dotnet test samples/Concepts --filter "FullyQualifiedName~Memory.VectorStore_VectorSearch_MultiStore_Chroma." --logger "console;verbosity=detailed"
dotnet test samples/Concepts --filter "FullyQualifiedName~Memory.VectorStore_DynamicDataModel_Interop_Chroma." --logger "console;verbosity=detailed"
dotnet test samples/Concepts --filter "FullyQualifiedName~Agents.ChatCompletion_Rag_Chroma." --logger "console;verbosity=detailed"
dotnet test samples/GettingStartedWithTextSearch --filter "FullyQualifiedName~Step5_Search_With_Chroma." --logger "console;verbosity=detailed"
```

The hybrid search sample runs on Chroma Cloud, which supports hybrid search, with the tenant and the database of its dashboard. It deletes its collection at the end:

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

[compose.yaml](./compose.yaml) runs Ollama and downloads `qwen2.5:3b` and `nomic-embed-text`, about 2 GB the first time; `docker compose logs -f ollama-models` shows the progress.

```bash
docker compose up -d
dotnet test samples/Concepts --filter "FullyQualifiedName~Agents.ChatCompletion_Rag_Chroma_Ollama." --logger "console;verbosity=detailed"
dotnet test samples/GettingStartedWithTextSearch --filter "FullyQualifiedName~Step5_Search_With_Chroma_Ollama." --logger "console;verbosity=detailed"
```

The defaults match the compose file. To change them, set `Ollama:Endpoint`, `Ollama:ModelId`, `Ollama:EmbeddingModelId` and `Ollama:EmbeddingDimensions`; the data model of `Step5_Search_With_Chroma_Ollama`, in [ChromaVectorStoreOllamaFixture.cs](./samples/GettingStartedWithTextSearch/ChromaVectorStoreOllamaFixture.cs), has the 768 dimensions of `nomic-embed-text` in its attribute. A larger chat model than `qwen2.5:3b` gives better answers: it answers from the documents, but it can add made-up links when they have no source.

## Tests

```bash
dotnet test test/ChromaDB.SemanticKernel.Sample.Tests
```

GitHub Actions runs them on every push to `main` and on every pull request to `main`.

The tests run the scenario of each sample except the hybrid search, which needs Chroma Cloud, with the same configuration, against Chroma in a container started on a free port with ChromaDotNet.Testcontainers. They need Docker, but no model: the embeddings come from word hashes and the chat model only records what it receives.

- Vector search: each search finds the glossary entry it asks about, and the filter on the category leaves out the entry of the other category, Connectors, with and without dependency injection.
- Data models: a record written as a dictionary reads back as the .NET data model, and the other way round, vector included.
- Paging: the pages go through each of the 1,000 records once, in the order of the distance.
- RAG: the document that answers each question reaches the model, and with a search namespace only the documents of the namespace reach it, with their source.
- Text search: the search returns the records with their key, text and link, the Handlebars prompt receives the search results, and with function calling the results of the search reach the model.

The files in [samples/InternalUtilities](./samples/InternalUtilities/), the common code of the vector store samples and the container helper come from the [Semantic Kernel repository](https://github.com/microsoft/semantic-kernel) (MIT), reduced to what these samples use, and keep their copyright notice. The samples and their fixtures are adaptations of the Semantic Kernel samples linked in the table above, under the same license, and carry the same copyright notice.
