using F1Predictor.Application.Features.Analysis;
using F1Predictor.Application.Features.Analysis.AskAnalyst;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace F1Predictor.Application.Tests.Analysis;

public sealed class AnalystStreamTests
{
    private static async IAsyncEnumerable<ChatResponseUpdate> Updates(params ChatResponseUpdate[] updates)
    {
        foreach (var u in updates) { await Task.Yield(); yield return u; }
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> Faulting()
    {
        await Task.Yield();
        yield return new ChatResponseUpdate(ChatRole.Assistant, "partial");
        throw new HttpRequestException("connection reset");
    }

    [Fact]
    public async Task Map_ToolCallThenText_EmitsStatusDeltasDone()
    {
        var call = new ChatResponseUpdate { Role = ChatRole.Assistant, Contents = [new FunctionCallContent("call1", "get_standings", null)] };
        var text1 = new ChatResponseUpdate(ChatRole.Assistant, "Max ");
        var text2 = new ChatResponseUpdate(ChatRole.Assistant, "leads.");

        var events = await AnalystStream.Map(Updates(call, text1, text2), NullLogger.Instance, CancellationToken.None).ToListAsync();

        events.Select(e => e.Type).Should().Equal("status", "delta", "delta", "done");
        events[0].Text.Should().Be(AnalystTools.StatusFor("get_standings"));
        string.Concat(events.Where(e => e.Type == "delta").Select(e => e.Text)).Should().Be("Max leads.");
    }

    [Fact]
    public async Task Map_ProviderThrows_EndsWithErrorEventNotException()
    {
        var events = await AnalystStream.Map(Faulting(), NullLogger.Instance, CancellationToken.None).ToListAsync();

        events.Select(e => e.Type).Should().Equal("delta", "error");
        events[^1].Text.Should().NotContain("connection reset");
    }

    [Fact]
    public async Task Map_FunctionResultContent_IsNotForwarded()
    {
        var result = new ChatResponseUpdate { Role = ChatRole.Tool, Contents = [new FunctionResultContent("call1", "{...}")] };

        var events = await AnalystStream.Map(Updates(result), NullLogger.Instance, CancellationToken.None).ToListAsync();

        events.Select(e => e.Type).Should().Equal("done");
    }
}
