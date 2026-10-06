using F1Predictor.Application.Abstractions.Messaging;

namespace F1Predictor.Application.Features.Predictions.GetTrainingRuns;

/// <summary>
/// The training-run log, newest first — the place to see whether an experiment helped.
/// </summary>
/// <param name="Target">"Podium" or "PointsFinish" (case-insensitive); null for both.</param>
/// <param name="Take">How many runs to return; clamped to 1..200.</param>
public sealed record GetTrainingRunsQuery(string? Target, int Take = 50) : IQuery<TrainingRunsResponse>;
