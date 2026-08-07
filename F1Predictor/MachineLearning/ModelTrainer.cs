using F1Predictor.Data;
using Microsoft.ML;

namespace F1Predictor.MachineLearning;

public class ModelTrainer
{
    private const int Seed = 42;

    private readonly MLContext _mlContext = new(seed: Seed);

    public ITransformer TrainAndSave(IEnumerable<DriverRaceFeature> trainingRows, bool usePodiumLabel, string modelPath)
    {
        var inputs = trainingRows.Select(f => new RaceFeatureInput
        {
            GridPosition = f.GridPosition,
            QualiGapToPole = f.QualiGapToPole,
            PitStopCount = f.PitStopCount,
            AvgPitStopDuration = f.AvgPitStopDuration,
            Rainfall = f.Rainfall,
            Label = usePodiumLabel ? f.Podium : f.PointsFinish
        });

        var data = _mlContext.Data.LoadFromEnumerable(inputs);
        var split = _mlContext.Data.TrainTestSplit(data, testFraction: 0.2, seed: Seed);

        var pipeline = _mlContext.Transforms
            .Concatenate("Features", nameof(RaceFeatureInput.GridPosition), nameof(RaceFeatureInput.QualiGapToPole),
                nameof(RaceFeatureInput.PitStopCount), nameof(RaceFeatureInput.AvgPitStopDuration), nameof(RaceFeatureInput.Rainfall))
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: "Label", featureColumnName: "Features"));

        var model = pipeline.Fit(split.TrainSet);

        var predictions = model.Transform(split.TestSet);
        var metrics = _mlContext.BinaryClassification.Evaluate(predictions, labelColumnName: "Label");

        var label = usePodiumLabel ? "Podium" : "Points-finish";
        Console.WriteLine($"=== {label} model metrics ===");
        Console.WriteLine("(AUC and F1 reported, not raw accuracy: the positive class is a small minority of rows,");
        Console.WriteLine(" so a model that always predicts \"no\" would already score high accuracy while being useless.)");
        Console.WriteLine($"AUC: {metrics.AreaUnderRocCurve:F3}");
        Console.WriteLine($"F1:  {metrics.F1Score:F3}");
        Console.WriteLine();

        _mlContext.Model.Save(model, data.Schema, modelPath);

        return model;
    }

    public RacePredictionOutput Predict(ITransformer model, DriverRaceFeature feature)
    {
        var engine = _mlContext.Model.CreatePredictionEngine<RaceFeatureInput, RacePredictionOutput>(model);
        return engine.Predict(new RaceFeatureInput
        {
            GridPosition = feature.GridPosition,
            QualiGapToPole = feature.QualiGapToPole,
            PitStopCount = feature.PitStopCount,
            AvgPitStopDuration = feature.AvgPitStopDuration,
            Rainfall = feature.Rainfall
        });
    }
}
