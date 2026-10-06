using F1Predictor.Application.Abstractions.MachineLearning;

namespace F1Predictor.Application.Features.Analysis;

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
