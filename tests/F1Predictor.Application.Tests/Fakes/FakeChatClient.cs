using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace F1Predictor.Application.Tests.Fakes;

internal sealed class FakeChatClient : IChatClient
{
    public string ReplyText { get; set; } = "Fake narrative.";

    public Exception? Throws { get; set; }

    public List<IList<ChatMessage>> Calls { get; } = [];

    public List<ChatOptions?> Options { get; } = [];

    public Func<IEnumerable<ChatResponseUpdate>>? StreamFactory { get; set; }

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls.Add([.. messages]);
        Options.Add(options);
        if (Throws is not null)
        {
            throw Throws;
        }

        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, ReplyText)));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Calls.Add([.. messages]);
        Options.Add(options);
        if (Throws is not null)
        {
            throw Throws;
        }

        foreach (var update in StreamFactory?.Invoke() ?? [new ChatResponseUpdate(ChatRole.Assistant, ReplyText)])
        {
            await Task.Yield();
            yield return update;
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
