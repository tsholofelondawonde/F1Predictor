using F1Predictor.Application.Abstractions.MachineLearning;
using F1Predictor.Domain.Predictions;
using F1Predictor.Infrastructure.MachineLearning;
using Microsoft.ML;
using Microsoft.ML.Calibrators;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;

namespace F1Predictor.Infrastructure.Tests.MachineLearning;

/// <summary>
/// Trains tiny models on synthetic rows where a low grid position strongly predicts a podium,
/// and writes them where <see cref="ModelStorageOptions"/> expects them.
/// </summary>
internal static class SyntheticModels
{
    public static List<DriverRaceFeature> Rows(int count = 400, int seed = 42)
    {
        var random = new Random(seed);
        var rows = new List<DriverRaceFeature>(count);

        for (var i = 0; i < count; i++)
        {
            var grid = random.Next(1, 21);
            var noise = random.NextDouble();

            rows.Add(new DriverRaceFeature
            {
                SessionKey = 1000 + i / 20,
                DriverNumber = i % 20 + 1,
                GridPosition = grid,
                QualiGapToPole = (float)(grid * 0.1 + noise * 0.2),
                PitStopCount = random.Next(1, 4),
                AvgPitStopDuration = (float)(22 + noise * 3),
                Rainfall = random.NextDouble() < 0.15 ? 1 : 0,
                FinishPosition = grid,
                Podium = grid <= 3 ? noise < 0.9 : noise < 0.05,
                PointsFinish = grid <= 10 ? noise < 0.9 : noise < 0.1
            });
        }

        return rows;
    }

    public static IDataView DataView(MLContext mlContext, IEnumerable<DriverRaceFeature> rows, PredictionTarget target) =>
        mlContext.Data.LoadFromEnumerable(rows.Select(r => RaceFeatureInput.From(
            r, target == PredictionTarget.Podium ? r.Podium : r.PointsFinish)));

    /// <summary>
    /// Linear pipeline: Concatenate → MinMax → SDCA. Returns the fitted chain and its bias.
    /// </summary>
    /// <remarks>
    /// <c>GridPosition</c> and <c>QualiGapToPole</c> are deliberately near-collinear here
    /// (<c>QualiGapToPole ≈ grid * 0.1</c>), which is realistic — a driver's gap to pole tracks
    /// their grid slot. Under SDCA's default (near-zero) L2 term, two collinear features aren't
    /// identifiable: the solver can push one coefficient strongly positive and its pair strongly
    /// negative and still fit the training data, because only their sum is pinned down, not each
    /// one individually. That produced a GridPosition coefficient with the "wrong" sign in
    /// practice — reproducible on this exact data and seed, not flaky — despite every score
    /// still summing correctly from its contributions (verified by the bias-plus-contributions
    /// test). A modest L2 term resolves the ambiguity the way ridge regression always does: it
    /// prefers the solution that spreads weight evenly across correlated predictors, which is
    /// also the intuitively "right" one here. Pinned to one thread so the fit — and which of the
    /// many equally-good solutions SDCA lands on — is reproducible across machines.
    /// </remarks>
    public static (ITransformer Model, float Bias) TrainLinear(MLContext mlContext, IDataView data)
    {
        var pipeline = mlContext.Transforms.Concatenate("Features", RaceFeatureInput.FeatureNames)
            .Append(mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(new SdcaLogisticRegressionBinaryTrainer.Options
            {
                LabelColumnName = "Label",
                L2Regularization = 0.5f,
                NumberOfThreads = 1
            }));

        var model = pipeline.Fit(data);
        var bias = model.LastTransformer.Model.SubModel.Bias;

        return (model, bias);
    }

    /// <summary>Tree pipeline: Concatenate → FastTree, which is what AutoML most often lands on.</summary>
    public static ITransformer TrainTree(MLContext mlContext, IDataView data)
    {
        var pipeline = mlContext.Transforms.Concatenate("Features", RaceFeatureInput.FeatureNames)
            .Append(mlContext.BinaryClassification.Trainers.FastTree(
                labelColumnName: "Label", numberOfLeaves: 4, numberOfTrees: 20, minimumExampleCountPerLeaf: 5));

        return pipeline.Fit(data);
    }

    /// <summary>Saves both target models to a fresh temp directory and returns its path.</summary>
    public static string SaveBoth(MLContext mlContext, IDataView schemaSource, ITransformer podium, ITransformer points)
    {
        var dir = Path.Combine(Path.GetTempPath(), "f1predictor-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        mlContext.Model.Save(podium, schemaSource.Schema, Path.Combine(dir, "podium-model.zip"));
        mlContext.Model.Save(points, schemaSource.Schema, Path.Combine(dir, "points-model.zip"));

        return dir;
    }
}
