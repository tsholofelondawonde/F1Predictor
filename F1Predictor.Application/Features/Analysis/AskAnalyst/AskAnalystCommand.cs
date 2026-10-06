using F1Predictor.Application.Abstractions.Messaging;

namespace F1Predictor.Application.Features.Analysis.AskAnalyst;

/// <param name="Role">"user" or "assistant". The system prompt is never client-supplied.</param>
public sealed record ChatTurn(string Role, string Content);

/// <summary>A question for the analyst, with the conversation so far. The reply is a stream of events.</summary>
public sealed record AskAnalystCommand(int Year, IReadOnlyList<ChatTurn> Messages) : ICommand<AnalystReply>;

public sealed record AnalystReply(IAsyncEnumerable<AnalystEvent> Events);
