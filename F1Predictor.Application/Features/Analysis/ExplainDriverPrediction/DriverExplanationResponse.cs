using F1Predictor.Application.Features.Analysis;

namespace F1Predictor.Application.Features.Analysis.ExplainDriverPrediction;

/// <param name="SessionKey">The next Grand Prix's race session key.</param>
/// <param name="MeetingName">The next Grand Prix's meeting name.</param>
/// <param name="GridConfirmed">
/// True when the explanation was computed from the real starting grid; false when it was
/// computed from a grid projected from recent form.
/// </param>
/// <param name="DriverNumber">The car number explained.</param>
/// <param name="FullName">The driver's full name.</param>
/// <param name="NameAcronym">The driver's three-letter acronym.</param>
/// <param name="TeamName">The driver's team.</param>
/// <param name="TeamColour">The team's colour, as a hex string with no leading '#'.</param>
/// <param name="Podium">Breakdown of the podium prediction.</param>
/// <param name="PointsFinish">Breakdown of the points-finish prediction.</param>
/// <param name="FinishPosition">Reserved for a future holdout comparison; always null for a next-race explanation.</param>
/// <param name="ActualPodium">Reserved for a future holdout comparison; always null for a next-race explanation.</param>
/// <param name="ActualPointsFinish">Reserved for a future holdout comparison; always null for a next-race explanation.</param>
/// <param name="Narrative">A short AI narrative of the contributions, or null when no AI provider is configured or it failed.</param>
/// <param name="Model">The chat model that produced <paramref name="Narrative"/>, or null when there is none.</param>
public sealed record DriverExplanationResponse(
    int SessionKey,
    string MeetingName,
    bool GridConfirmed,
    int DriverNumber,
    string FullName,
    string NameAcronym,
    string TeamName,
    string TeamColour,
    TargetExplanationResponse Podium,
    TargetExplanationResponse PointsFinish,
    int? FinishPosition,
    bool? ActualPodium,
    bool? ActualPointsFinish,
    string? Narrative,
    string? Model);
