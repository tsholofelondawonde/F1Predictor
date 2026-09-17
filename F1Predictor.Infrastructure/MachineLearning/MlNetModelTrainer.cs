using F1Predictor.Application.Abstractions.MachineLearning;
using F1Predictor.Domain.Predictions;
using Microsoft.Extensions.Options;
using Microsoft.ML;
using Microsoft.ML.AutoML;

namespace F1Predictor.Infrastructure.MachineLearning;

/// <summary>
/// Trains the two binary classifiers via an ML.NET AutoML search over trainers (SDCA, LightGBM,
/// FastTree, FastForest, LBFGS), optimizing F1.
/// </summary>
/// <remarks>
/// The search runs against a fixed 30-second-per-classifier budget and a seeded <see
/// cref="MLContext"/>, but AutoML's own trainer/hyperparameter search is best-effort
/// reproducible only — unlike the fixed SDCA pipeline this replaced, re-running training is not
/// guaranteed to reach the same trainer or metrics bit-for-bit. The 80/20 split, evaluation, and
/// model persistence around the search are unchanged.
/// </remarks>
internal sealed class MlNetModelTrainer(IOptions<ModelStorageOptions> options) : IModelTrainer
{
    /// <summary>Fixed so that repeated runs on the same data give the same train/test split.</summary>
    private const int Seed = 42;

    private const double TestFraction = 0.2;
    private const uint MaxExperimentTimeInSeconds = 30;

    private readonly MLContext _mlContext = new(seed: Seed);
    private readonly ModelStorageOptions _options = options.Value;

    public ModelTrainingResult Train(IReadOnlyCollection<DriverRaceFeature> trainingRows, PredictionTarget target)
    {
        ArgumentNullException.ThrowIfNull(trainingRows);

        var inputs = trainingRows.Select(f => RaceFeatureInput.From(f, LabelFor(f, target)));

        var data = _mlContext.Data.LoadFromEnumerable(inputs);
        var split = _mlContext.Data.TrainTestSplit(data, TestFraction, seed: Seed);

        var experimentResult = _mlContext.Auto()
            .CreateBinaryClassificationExperiment(new BinaryExperimentSettings
            {
                MaxExperimentTimeInSeconds = MaxExperimentTimeInSeconds,
                OptimizingMetric = BinaryClassificationMetric.F1Score
            })
            .Execute(split.TrainSet, labelColumnName: "Label");

        var model = experimentResult.BestRun.Model;

        var predictions = model.Transform(split.TestSet);
        var metrics = _mlContext.BinaryClassification.Evaluate(predictions, labelColumnName: "Label");

        var modelPath = _options.PathFor(target);
        Directory.CreateDirectory(Path.GetDirectoryName(modelPath)!);
        _mlContext.Model.Save(model, data.Schema, modelPath);

        return new ModelTrainingResult(
            target,
            metrics.AreaUnderRocCurve,
            metrics.F1Score,
            trainingRows.Count,
            modelPath,
            experimentResult.BestRun.TrainerName);
    }

    private static bool LabelFor(DriverRaceFeature feature, PredictionTarget target) => target switch
    {
        PredictionTarget.Podium => feature.Podium,
        PredictionTarget.PointsFinish => feature.PointsFinish,
        _ => throw new ArgumentOutOfRangeException(nameof(target), target, "Unknown prediction target.")
    };
}
