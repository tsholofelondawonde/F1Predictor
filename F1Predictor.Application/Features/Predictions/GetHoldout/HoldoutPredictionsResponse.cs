namespace F1Predictor.Application.Features.Predictions.GetHoldout;

/// <param name="Year">Season the holdout race belongs to.</param>
/// <param name="SessionKey">OpenF1 session key of the holdout race.</param>
/// <param name="MeetingName">Race weekend name.</param>
/// <param name="CircuitShortName">Short circuit name.</param>
/// <param name="DateStart">When the race started.</param>
/// <param name="Drivers">One row per classified driver, ordered by actual finishing position.</param>
public sealed record HoldoutPredictionsResponse(
    int Year,
    int SessionKey,
    string MeetingName,
    string CircuitShortName,
    DateTimeOffset DateStart,
    IReadOnlyList<DriverPrediction> Drivers);

/// <param name="DriverNumber">Car number.</param>
/// <param name="FullName">Driver's full name, or a car-number placeholder if no entry list was ingested.</param>
/// <param name="NameAcronym">Three-letter abbreviation.</param>
/// <param name="TeamName">Constructor they drove for.</param>
/// <param name="TeamColour">Six-digit hex team colour, no leading hash.</param>
/// <param name="GridPosition">Where they started.</param>
/// <param name="FinishPosition">Where they actually finished.</param>
/// <param name="ActualPodium">Whether they actually made the podium.</param>
/// <param name="ActualPointsFinish">Whether they actually scored points.</param>
/// <param name="PodiumProbability">Model's predicted podium probability.</param>
/// <param name="PointsProbability">Model's predicted points-finish probability.</param>
public sealed record DriverPrediction(
    int DriverNumber,
    string FullName,
    string NameAcronym,
    string TeamName,
    string TeamColour,
    float GridPosition,
    int FinishPosition,
    bool ActualPodium,
    bool ActualPointsFinish,
    float PodiumProbability,
    float PointsProbability);
