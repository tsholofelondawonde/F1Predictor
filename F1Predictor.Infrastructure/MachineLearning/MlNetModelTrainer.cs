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
/// The search fits on the earlier races and is scored on the later ones, exactly as handed over
/// in <see cref="TrainingData"/> — this class never re-splits. Once the metrics are taken, the
/// winning pipeline is refitted on train + validation together and <em>that</em> is saved, so
/// the served model has seen every non-holdout race while the reported numbers still come from
/// races it had not seen.
/// <para>
/// The search runs against a fixed 30-second-per-classifier budget and a seeded
/// <see cref="MLContext"/>, but AutoML's trainer/hyperparameter search is best-effort
/// reproducible only: re-running is not guaranteed to reach the same trainer or metrics.
/// </para>
/// </remarks>
internal sealed class MlNetModelTrainer(IOptions<ModelStorageOptions> options) : IModelTrainer
{
    private const int Seed = 42;
    private const uint MaxExperimentTimeInSeconds = 30;

    private readonly MLContext _mlContext = new(seed: Seed);
    private readonly ModelStorageOptions _options = options.Value;

    public ModelTrainingResult Train(TrainingData data, PredictionTarget target)
    {
        ArgumentNullException.ThrowIfNull(data);

        var trainView = Load(data.Train, target);
        var validationView = Load(data.Validation, target);

        var experimentResult = _mlContext.Auto()
            .CreateBinaryClassificationExperiment(new BinaryExperimentSettings
            {
                MaxExperimentTimeInSeconds = MaxExperimentTimeInSeconds,
                OptimizingMetric = BinaryClassificationMetric.F1Score
            })
            .Execute(trainView, validationView, labelColumnName: "Label");

        var bestRun = experimentResult.BestRun;

        // Scored with the model fitted on the train side only — the honest number.
        var predictions = bestRun.Model.Transform(validationView);
        var metrics = _mlContext.BinaryClassification.Evaluate(predictions, labelColumnName: "Label");

        // Then the same pipeline, refitted on everything, is what gets shipped.
        var allView = Load([.. data.Train, .. data.Validation], target);
        var finalModel = bestRun.Estimator.Fit(allView);

        var modelPath = _options.PathFor(target);
        Directory.CreateDirectory(Path.GetDirectoryName(modelPath)!);
        _mlContext.Model.Save(finalModel, allView.Schema, modelPath);

        return new ModelTrainingResult(
            target,
            metrics.AreaUnderRocCurve,
            metrics.F1Score,
            data.Train.Count,
            modelPath,
            bestRun.TrainerName,
            data.Validation.Count,
            metrics.LogLoss,
            metrics.AreaUnderPrecisionRecallCurve,
            metrics.PositivePrecision,
            metrics.PositiveRecall,
            RaceFeatureInput.FeatureNames);
    }

    private IDataView Load(IEnumerable<DriverRaceFeature> rows, PredictionTarget target) =>
        _mlContext.Data.LoadFromEnumerable(rows.Select(f => RaceFeatureInput.From(f, LabelFor(f, target))));

    private static bool LabelFor(DriverRaceFeature feature, PredictionTarget target) => target switch
    {
        PredictionTarget.Podium => feature.Podium,
        PredictionTarget.PointsFinish => feature.PointsFinish,
        _ => throw new ArgumentOutOfRangeException(nameof(target), target, "Unknown prediction target.")
    };
}
