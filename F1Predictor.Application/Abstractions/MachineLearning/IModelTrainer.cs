using F1Predictor.Domain.Predictions;

namespace F1Predictor.Application.Abstractions.MachineLearning;

/// <summary>
/// Trains and persists the binary classifiers.
/// </summary>
public interface IModelTrainer
{
    /// <summary>
    /// Searches for a classifier on <see cref="TrainingData.Train"/>, scores it against
    /// <see cref="TrainingData.Validation"/>, then refits the winning pipeline on both before
    /// saving it — so the metrics are honest and the shipped model still uses every row.
    /// </summary>
    /// <remarks>
    /// The split itself is the caller's policy (see <c>RaceTimeSplit</c>): it decides which
    /// races are "the future", and this port never shuffles rows across that line.
    /// </remarks>
    ModelTrainingResult Train(TrainingData data, PredictionTarget target);
}

/// <summary>
/// Feature rows already split by race and by time. No race appears on both sides.
/// </summary>
/// <param name="Train">Earlier races — what the search fits on.</param>
/// <param name="Validation">Later races — what the reported metrics are measured on.</param>
public sealed record TrainingData(
    IReadOnlyList<DriverRaceFeature> Train,
    IReadOnlyList<DriverRaceFeature> Validation);

/// <summary>
/// Validation metrics for one trained classifier.
/// </summary>
/// <param name="Target">Which label this model predicts.</param>
/// <param name="AreaUnderRocCurve">
/// AUC on the validation races. This, not accuracy, is the number to judge the model by: podium
/// is roughly 15% of rows, so a model that always answers "no" scores ~85% accuracy while being
/// worthless. 0.5 is noise; grid position alone should manage well above 0.65.
/// </param>
/// <param name="F1Score">F1 on the validation races, for the same minority-class reason.</param>
/// <param name="TrainingRowCount">Rows the search fitted on (the train side only).</param>
/// <param name="ModelPath">Absolute path of the saved model file.</param>
/// <param name="TrainerName">
/// The AutoML-selected trainer (e.g. "LightGbmBinary", "SdcaLogisticRegressionBinary") — the
/// two targets are trained independently and may land on different trainers.
/// </param>
/// <param name="ValidationRowCount">Rows the metrics were measured on.</param>
/// <param name="LogLoss">
/// Validation log loss in bits. Where AUC only asks "are the rows in the right order?", this
/// asks "are the probabilities themselves believable?" — lower is better, 1.0 is a coin flip.
/// </param>
/// <param name="AreaUnderPrecisionRecallCurve">
/// Validation AUPRC. More sensitive than AUC when positives are rare; a no-skill model scores
/// roughly the positive rate (~0.15 for podium), not 0.5.
/// </param>
/// <param name="PositivePrecision">Of the rows predicted positive, the share that were.</param>
/// <param name="PositiveRecall">Of the rows that were positive, the share predicted so.</param>
/// <param name="FeatureNames">The feature columns the model was trained on, in order.</param>
public sealed record ModelTrainingResult(
    PredictionTarget Target,
    double AreaUnderRocCurve,
    double F1Score,
    int TrainingRowCount,
    string ModelPath,
    string TrainerName,
    int ValidationRowCount,
    double LogLoss,
    double AreaUnderPrecisionRecallCurve,
    double PositivePrecision,
    double PositiveRecall,
    IReadOnlyList<string> FeatureNames);
