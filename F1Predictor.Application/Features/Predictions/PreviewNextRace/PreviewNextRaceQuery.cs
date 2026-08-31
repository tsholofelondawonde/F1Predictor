using F1Predictor.Application.Abstractions.Messaging;

namespace F1Predictor.Application.Features.Predictions.PreviewNextRace;

/// <summary>
/// Podium and points probabilities for the next Grand Prix that has not been run.
/// </summary>
public sealed record PreviewNextRaceQuery(int Year) : IQuery<NextRacePreviewResponse>;
