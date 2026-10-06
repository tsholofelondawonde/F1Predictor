using F1Predictor.Domain.Predictions;

namespace F1Predictor.Application.Abstractions.MachineLearning;

/// <summary>
/// Serves predictions from the saved models.
/// </summary>
public interface IRacePredictor
{
    /// <summary>
    /// True once both models have been trained and saved. Predictions cannot be served before then.
    /// </summary>
    bool ModelsAvailable { get; }

    /// <summary>
    /// Podium and points-finish probabilities for one driver-race row.
    /// </summary>
    RaceProbabilities Predict(DriverRaceFeature feature);

    /// <summary>
    /// The same two predictions, broken down into what each feature contributed to the score.
    /// Computed by the model itself (ML.NET feature-contribution calculation), never estimated.
    /// </summary>
    RaceExplanation Explain(DriverRaceFeature feature);
}

/// <param name="PodiumProbability">Probability this driver finishes in the top three.</param>
/// <param name="PointsProbability">Probability this driver finishes in the points.</param>
public sealed record RaceProbabilities(float PodiumProbability, float PointsProbability);

/// <param name="Podium">Breakdown of the podium prediction.</param>
/// <param name="PointsFinish">Breakdown of the points-finish prediction.</param>
public sealed record RaceExplanation(TargetExplanation Podium, TargetExplanation PointsFinish);

/// <param name="Probability">Calibrated probability, identical to what <see cref="IRacePredictor.Predict"/> returns.</param>
/// <param name="Score">Raw model score the probability was calibrated from.</param>
/// <param name="Contributions">One entry per feature the model consumed, in the model's slot order.</param>
public sealed record TargetExplanation(
    float Probability,
    float Score,
    IReadOnlyList<FeatureContribution> Contributions);

/// <param name="Feature">Feature name as the model knows it, e.g. "GridPosition".</param>
/// <param name="Value">The raw feature value that was fed in.</param>
/// <param name="Contribution">
/// Contribution to <see cref="TargetExplanation.Score"/>. Positive pushes toward the positive class.
/// For a linear model these sum, with the bias, to the score; for a tree ensemble they are the
/// per-feature path contributions on the same scale.
/// </param>
public sealed record FeatureContribution(string Feature, float Value, float Contribution);
