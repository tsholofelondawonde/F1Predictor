using Microsoft.Extensions.AI;

namespace F1Predictor.Infrastructure.AI;

/// <summary>
/// Registered when no provider is configured so handler constructors always resolve; any call
/// is a programmer error, exactly like predicting before the models exist. Handlers guard on
/// <see cref="Application.Abstractions.AI.IAiCapabilities.ChatAvailable"/> first.
/// </summary>
internal sealed class UnavailableChatClient : IChatClient
{
    private const string Message = "No AI provider is configured (Ai:Provider is None). Check IAiCapabilities.ChatAvailable before calling the chat client.";

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(Message);

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(Message);

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
