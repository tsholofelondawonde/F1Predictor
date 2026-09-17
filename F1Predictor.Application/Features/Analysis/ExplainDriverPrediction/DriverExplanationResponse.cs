using F1Predictor.Application.Abstractions.MachineLearning;

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

/// <param name="Probability">Calibrated probability for this target.</param>
/// <param name="Score">Raw model score the probability was calibrated from.</param>
/// <param name="Contributions">One entry per feature the model consumed.</param>
public sealed record TargetExplanationResponse(float Probability, float Score, IReadOnlyList<FeatureContributionResponse> Contributions)
{
    public static TargetExplanationResponse From(TargetExplanation explanation) => new(
        explanation.Probability,
        explanation.Score,
        [.. explanation.Contributions.Select(c => new FeatureContributionResponse(
            c.Feature, c.Value, c.Contribution, DirectionOf(c.Contribution)))]);

    private static string DirectionOf(float contribution) => contribution switch
    {
        > 0 => "helps",
        < 0 => "hurts",
        _ => "neutral"
    };
}

/// <param name="Feature">Feature name as the model knows it, e.g. "GridPosition".</param>
/// <param name="Value">The raw feature value that was fed in.</param>
/// <param name="Contribution">Contribution to <see cref="TargetExplanationResponse.Score"/>.</param>
/// <param name="Direction">"helps", "hurts" or "neutral", derived from the sign of <paramref name="Contribution"/>.</param>
public sealed record FeatureContributionResponse(string Feature, float Value, float Contribution, string Direction);
