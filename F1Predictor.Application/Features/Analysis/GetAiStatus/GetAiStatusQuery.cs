using F1Predictor.Application.Abstractions.Messaging;

namespace F1Predictor.Application.Features.Analysis.GetAiStatus;

public sealed record GetAiStatusQuery : IQuery<AiStatusResponse>;
