using F1Predictor.Application.Abstractions.MachineLearning;

namespace F1Predictor.Application.Features.Predictions.Train;

/// <param name="Year">Season trained on.</param>
/// <param name="RacesTrainedOn">Races contributing training rows, excluding the holdout.</param>
/// <param name="TrainingRows">Driver-race rows the models were fitted on.</param>
/// <param name="HoldoutRaceName">The race excluded from training.</param>
/// <param name="Podium">Metrics for the podium classifier.</param>
/// <param name="PointsFinish">Metrics for the points-finish classifier.</param>
/// <param name="MetricGuidance">
/// Why the reported metrics are AUC and F1 rather than accuracy. Surfaced in the response
/// rather than left in a comment because the number a reader reaches for first is usually
/// the misleading one.
/// </param>
public sealed record TrainModelsResponse(
    int Year,
    int RacesTrainedOn,
    int TrainingRows,
    string HoldoutRaceName,
    ModelTrainingResult Podium,
    ModelTrainingResult PointsFinish,
    string MetricGuidance);
