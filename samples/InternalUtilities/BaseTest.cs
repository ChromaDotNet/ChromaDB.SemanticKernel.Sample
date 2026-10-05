// Copyright (c) Microsoft. All rights reserved.

using System.Reflection;
using System.Text;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

public abstract class BaseTest : TextWriter
{
    protected ITestOutputHelper Output { get; }

    /// <summary>
    /// This property makes the samples Console friendly. Allowing them to be copied and pasted into a Console app, with minimal changes.
    /// </summary>
    public BaseTest Console => this;

    protected Kernel CreateKernelWithChatCompletion(string? modelName = null)
    {
        var builder = Kernel.CreateBuilder();

        AddChatCompletionToKernel(builder, modelName);

        return builder.Build();
    }

    protected void AddChatCompletionToKernel(IKernelBuilder builder, string? modelName = null)
    {
        if (!string.IsNullOrEmpty(TestConfiguration.AzureOpenAI.ApiKey))
        {
            builder.AddAzureOpenAIChatCompletion(
                modelName ?? TestConfiguration.AzureOpenAI.ChatDeploymentName,
                TestConfiguration.AzureOpenAI.Endpoint,
                TestConfiguration.AzureOpenAI.ApiKey);
        }
        else
        {
            builder.AddAzureOpenAIChatCompletion(
                modelName ?? TestConfiguration.AzureOpenAI.ChatDeploymentName,
                TestConfiguration.AzureOpenAI.Endpoint,
                new AzureCliCredential());
        }
    }

    protected BaseTest(ITestOutputHelper output)
    {
        this.Output = output;

        IConfigurationRoot configRoot = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Development.json", true)
            .AddEnvironmentVariables()
            .AddUserSecrets(Assembly.GetExecutingAssembly())
            .Build();

        TestConfiguration.Initialize(configRoot);
    }

    /// <inheritdoc/>
    public override void WriteLine(object? value = null)
        => this.Output.WriteLine(value ?? string.Empty);

    /// <inheritdoc/>
    public override void WriteLine(string? format, params object?[] arg)
        => this.Output.WriteLine(format ?? string.Empty, arg);

    /// <inheritdoc/>
    public override void WriteLine(string? value)
        => this.Output.WriteLine(value ?? string.Empty);

    /// <inheritdoc/>
    /// <remarks>
    /// <see cref="ITestOutputHelper"/> only supports output that ends with a newline.
    /// User this method will resolve in a call to <see cref="WriteLine(string?)"/>.
    /// </remarks>
    public override void Write(object? value = null)
        => this.Output.WriteLine(value ?? string.Empty);

    /// <inheritdoc/>
    /// <remarks>
    /// <see cref="ITestOutputHelper"/> only supports output that ends with a newline.
    /// User this method will resolve in a call to <see cref="WriteLine(string?)"/>.
    /// </remarks>
    public override void Write(char[]? buffer)
        => this.Output.WriteLine(new string(buffer));

    /// <inheritdoc/>
    public override Encoding Encoding => Encoding.UTF8;
}
