using Microsoft.Extensions.AI;

namespace ChromaDB.SemanticKernel.Sample.Tests;

/// <summary>
/// A chat model that first calls the only tool it receives with the given arguments,
/// then answers "Noted." and records the messages it receives, the result of the tool included.
/// </summary>
public sealed class FunctionCallingChatClient(Dictionary<string, object?> arguments) : IChatClient
{
    public string? CalledToolName { get; private set; }

    public IReadOnlyList<ChatMessage> LastMessages { get; private set; } = [];

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (CalledToolName is null)
        {
            CalledToolName = Assert.Single(options?.Tools ?? []).Name;
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("call-1", CalledToolName, arguments)])));
        }

        LastMessages = messages.ToList();
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Noted.")));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var update in (await GetResponseAsync(messages, options, cancellationToken)).ToChatResponseUpdates())
        {
            yield return update;
        }
    }

    // Semantic Kernel reads the metadata of the chat client before it renders a prompt.
    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceType == typeof(ChatClientMetadata) ? new ChatClientMetadata(nameof(FunctionCallingChatClient)) : null;

    public void Dispose()
    {
    }
}
