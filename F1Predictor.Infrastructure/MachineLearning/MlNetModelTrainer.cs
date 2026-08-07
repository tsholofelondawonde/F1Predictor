using F1Predictor.Application.Abstractions.MachineLearning;
using F1Predictor.Domain.Predictions;
using Microsoft.Extensions.Options;
using Microsoft.ML;

namespace F1Predictor.Infrastructure.MachineLearning;

/// <summary>
/// Trains the two binary classifiers with a hand-built SDCA logistic regression pipeline.
/// </summary>
/// <remarks>
/// Deliberately not AutoML: ML.NET's AutoML API is still marked preview, and a first working
/// run should not depend on a surface that may shift between package versions. Swapping in
/// <c>mlContext.Auto()</c> later means replacing pipeline construction only — the split,
/// evaluation and persistence around it stay as they are.
/// </remarks>
internal sealed class MlNetModelTrainer(IOptions<ModelStorageOptions> options) : IModelTrainer
{
    /// <summary>Fixed so that repeated runs on the same data give the same model and metrics.</summary>
    private const int Seed = 42;

    private const double TestFraction = 0.2;

    private readonly MLContext _mlContext = new(seed: Seed);
    private readonly ModelStorageOptions _options = options.Value;

    public ModelTrainingResult Train(IReadOnlyCollection<DriverRaceFeature> trainingRows, PredictionTarget target)
    {
        ArgumentNullException.ThrowIfNull(trainingRows);

        var inputs = trainingRows.Select(f => RaceFeatureInput.From(f, LabelFor(f, target)));

        var data = _mlContext.Data.LoadFromEnumerable(inputs);
        var split = _mlContext.Data.TrainTestSplit(data, TestFraction, seed: Seed);

        var pipeline = _mlContext.Transforms
            .Concatenate("Features", RaceFeatureInput.FeatureColumns)
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(
                labelColumnName: "Label",
                featureColumnName: "Features"));

        var model = pipeline.Fit(split.TrainSet);

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
            modelPath);
    }

    private static bool LabelFor(DriverRaceFeature feature, PredictionTarget target) => target switch
    {
        PredictionTarget.Podium => feature.Podium,
        PredictionTarget.PointsFinish => feature.PointsFinish,
        _ => throw new ArgumentOutOfRangeException(nameof(target), target, "Unknown prediction target.")
    };
}
