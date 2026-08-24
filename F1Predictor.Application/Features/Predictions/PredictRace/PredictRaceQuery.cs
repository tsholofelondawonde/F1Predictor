using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Predictions.GetHoldout;

namespace F1Predictor.Application.Features.Predictions.PredictRace;

/// <summary>
/// Predictions for every driver in one specific race.
/// </summary>
public sealed record PredictRaceQuery(int SessionKey) : IQuery<RacePredictionsResponse>;

/// <param name="SessionKey">OpenF1 session key of the race.</param>
/// <param name="MeetingName">Race weekend name.</param>
/// <param name="Drivers">One row per driver, ordered by predicted podium probability, highest first.</param>
public sealed record RacePredictionsResponse(
    int SessionKey,
    string MeetingName,
    IReadOnlyList<DriverPrediction> Drivers);
