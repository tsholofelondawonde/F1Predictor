using F1Predictor.Domain.Predictions;

namespace F1Predictor.Application.Abstractions.MachineLearning;

/// <summary>
/// Trains and persists the binary classifiers.
/// </summary>
public interface IModelTrainer
{
    /// <summary>
    /// Trains one classifier on an 80/20 split of <paramref name="trainingRows"/>, evaluates it
    /// against the held-back 20%, saves it, and returns its metrics.
    /// </summary>
    ModelTrainingResult Train(IReadOnlyCollection<DriverRaceFeature> trainingRows, PredictionTarget target);
}

/// <summary>
/// Evaluation metrics for one trained classifier.
/// </summary>
/// <param name="Target">Which label this model predicts.</param>
/// <param name="AreaUnderRocCurve">
/// AUC on the test split. This, not accuracy, is the number to judge the model by: podium is
/// roughly 15% of rows, so a model that always answers "no" scores ~85% accuracy while being
/// worthless. Below 0.5 is noise; grid position alone should manage well above 0.65.
/// </param>
/// <param name="F1Score">F1 on the test split, for the same minority-class reason.</param>
/// <param name="TrainingRowCount">Rows the model was fitted on, before the test split.</param>
/// <param name="ModelPath">Absolute path of the saved model file.</param>
public sealed record ModelTrainingResult(
    PredictionTarget Target,
    double AreaUnderRocCurve,
    double F1Score,
    int TrainingRowCount,
    string ModelPath);
