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
}

/// <param name="PodiumProbability">Probability this driver finishes in the top three.</param>
/// <param name="PointsProbability">Probability this driver finishes in the points.</param>
public sealed record RaceProbabilities(float PodiumProbability, float PointsProbability);
