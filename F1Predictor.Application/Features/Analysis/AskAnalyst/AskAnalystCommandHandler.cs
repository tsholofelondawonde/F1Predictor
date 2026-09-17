using F1Predictor.Application.Abstractions.AI;
using F1Predictor.Application.Abstractions.MachineLearning;
using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Predictions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace F1Predictor.Application.Features.Analysis.AskAnalyst;

internal sealed class AskAnalystCommandHandler(
    IChatClient chatClient, IAiCapabilities ai, IRacePredictor predictor, AnalystTools tools,
    IDateTimeProvider clock, ILogger<AskAnalystCommandHandler> logger)
    : ICommandHandler<AskAnalystCommand, AnalystReply>
{
    // The LoggingDecorator logs "Completed command" when this returns — i.e. when the stream is
    // handed back, not when it finishes. Acceptable: per-token completion is not a log line.
    public Task<Result<AnalystReply>> Handle(AskAnalystCommand command, CancellationToken cancellationToken)
    {
        if (!ai.ChatAvailable) return Task.FromResult(Result.Failure<AnalystReply>(AnalysisErrors.AiUnavailable));
        if (!predictor.ModelsAvailable) return Task.FromResult(Result.Failure<AnalystReply>(PredictionErrors.ModelsNotTrained));

        var today = DateOnly.FromDateTime(clock.UtcNow);
        List<ChatMessage> messages = [new ChatMessage(ChatRole.System, AnalystPrompts.AnalystSystem(command.Year, today))];
        messages.AddRange(command.Messages.Select(t =>
            new ChatMessage(string.Equals(t.Role, "assistant", StringComparison.Ordinal) ? ChatRole.Assistant : ChatRole.User, t.Content)));

        var options = new ChatOptions { Tools = tools.For(command.Year) };
        var updates = chatClient.GetStreamingResponseAsync(messages, options, cancellationToken);

        return Task.FromResult(Result.Success(new AnalystReply(AnalystStream.Map(updates, logger, cancellationToken))));
    }
}
