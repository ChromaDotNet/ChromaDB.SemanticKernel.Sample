// Copyright (c) Microsoft. All rights reserved.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;

public sealed class TestConfiguration
{
    private readonly IConfigurationRoot _configRoot;
    private static TestConfiguration? s_instance;

    private TestConfiguration(IConfigurationRoot configRoot)
    {
        this._configRoot = configRoot;
    }

    public static void Initialize(IConfigurationRoot configRoot)
    {
        s_instance = new TestConfiguration(configRoot);
    }

    public static IConfigurationRoot? ConfigurationRoot => s_instance?._configRoot;
    public static OllamaConfig Ollama => LoadSection<OllamaConfig>(optional: true);
    public static AzureOpenAIConfig AzureOpenAI => LoadSection<AzureOpenAIConfig>();
    public static AzureOpenAIEmbeddingsConfig AzureOpenAIEmbeddings => LoadSection<AzureOpenAIEmbeddingsConfig>();
    public static ChromaConfig Chroma => LoadSection<ChromaConfig>();

    private static T LoadSection<T>(bool optional = false, [CallerMemberName] string? caller = null) where T : new()
    {
        if (s_instance is null)
        {
            throw new InvalidOperationException(
                "TestConfiguration must be initialized with a call to Initialize(IConfigurationRoot) before accessing configuration values.");
        }

        if (string.IsNullOrEmpty(caller))
        {
            throw new ArgumentNullException(nameof(caller));
        }

        return s_instance._configRoot.GetSection(caller).Get<T>() ??
               (optional ? new T() : throw new ConfigurationNotFoundException(section: caller));
    }

    public class AzureOpenAIConfig
    {
        public string ChatDeploymentName { get; set; }
        public string Endpoint { get; set; }
        public string ApiKey { get; set; }
    }

    public class AzureOpenAIEmbeddingsConfig
    {
        public string DeploymentName { get; set; }
        public string Endpoint { get; set; }
        public string ApiKey { get; set; }
    }

    /// <summary>
    /// A Chroma server, like Chroma Cloud with the tenant and the database of its dashboard.
    /// </summary>
    public class ChromaConfig
    {
        public string Endpoint { get; set; }
        public string ApiKey { get; set; }
        public string Tenant { get; set; }
        public string Database { get; set; }
    }

    /// <summary>
    /// The defaults match the models that the compose file of the repository downloads.
    /// </summary>
    public class OllamaConfig
    {
        public string ModelId { get; set; } = "qwen2.5:3b";
        public string EmbeddingModelId { get; set; } = "nomic-embed-text";
        public int EmbeddingDimensions { get; set; } = 768;

        public string Endpoint { get; set; } = "http://localhost:11434";
    }
}
