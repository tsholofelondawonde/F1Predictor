using F1Predictor.Application.Abstractions.MachineLearning;

namespace F1Predictor.Application.Features.Predictions.Train;

/// <param name="Year">Last season trained on; the holdout race is from it.</param>
/// <param name="RacesTrainedOn">Races contributing rows (train + validation), excluding the holdout.</param>
/// <param name="TrainingRows">Driver-race rows the final models were fitted on (train + validation).</param>
/// <param name="HoldoutRaceName">The race excluded from training.</param>
/// <param name="Podium">Validation metrics for the podium classifier.</param>
/// <param name="PointsFinish">Validation metrics for the points-finish classifier.</param>
/// <param name="MetricGuidance">
/// How to read the reported metrics. Surfaced in the response rather than left in a comment
/// because the number a reader reaches for first is usually the misleading one.
/// </param>
/// <param name="FromYear">First season trained on.</param>
/// <param name="ValidationRaceNames">The later races the metrics were measured on, oldest first.</param>
/// <param name="PodiumEvaluation">Baseline and holdout comparison for the podium model.</param>
/// <param name="PointsFinishEvaluation">Baseline and holdout comparison for the points-finish model.</param>
public sealed record TrainModelsResponse(
    int Year,
    int RacesTrainedOn,
    int TrainingRows,
    string HoldoutRaceName,
    ModelTrainingResult Podium,
    ModelTrainingResult PointsFinish,
    string MetricGuidance,
    int FromYear,
    IReadOnlyList<string> ValidationRaceNames,
    TargetEvaluation PodiumEvaluation,
    TargetEvaluation PointsFinishEvaluation);

/// <param name="RunId">The <c>ModelTrainingRuns</c> row this result was recorded as.</param>
/// <param name="BaselineAuc">
/// AUC of ranking the validation rows by grid position alone. The model's validation AUC has to
/// beat this to have learned anything the grid didn't already say. Null when undefined.
/// </param>
/// <param name="HoldoutAuc">AUC on the held-out race through the saved model; noisy. Null when undefined.</param>
/// <param name="HoldoutLogLoss">Log loss in bits on the held-out race. Null when there were no rows.</param>
public sealed record TargetEvaluation(
    int RunId,
    double? BaselineAuc,
    double? HoldoutAuc,
    double? HoldoutLogLoss);
