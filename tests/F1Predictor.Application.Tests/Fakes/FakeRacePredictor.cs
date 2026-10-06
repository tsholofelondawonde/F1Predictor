using F1Predictor.Application.Abstractions.MachineLearning;
using F1Predictor.Domain.Predictions;

namespace F1Predictor.Application.Tests.Fakes;

internal sealed class FakeRacePredictor : IRacePredictor
{
    public bool ModelsAvailable { get; set; } = true;

    public RaceProbabilities Predict(DriverRaceFeature feature) => new(0.5f, 0.7f);

    public RaceExplanation Explain(DriverRaceFeature feature)
    {
        var contributions = BuildContributions(feature);
        var score = contributions.Sum(c => c.Contribution);

        return new RaceExplanation(
            new TargetExplanation(0.5f, score, contributions),
            new TargetExplanation(0.7f, score, contributions));
    }

    private static IReadOnlyList<FeatureContribution> BuildContributions(DriverRaceFeature feature) =>
    [
        new FeatureContribution("GridPosition", feature.GridPosition, 1.2f - (0.1f * feature.GridPosition)),
        new FeatureContribution("QualiGapToPole", feature.QualiGapToPole, -0.3f * feature.QualiGapToPole),
        new FeatureContribution("PitStopCount", feature.PitStopCount, 0f),
        new FeatureContribution("AvgPitStopDuration", feature.AvgPitStopDuration, -0.05f),
        new FeatureContribution("Rainfall", feature.Rainfall, 0f),
    ];
}
