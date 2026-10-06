using F1Predictor.Application.Abstractions.MachineLearning;

namespace F1Predictor.Application.Tests.Fakes;

/// <summary>Records what it was asked to train on and returns fixed metrics.</summary>
internal sealed class FakeModelTrainer : IModelTrainer
{
    public List<(TrainingData Data, PredictionTarget Target)> Calls { get; } = [];

    public ModelTrainingResult Train(TrainingData data, PredictionTarget target)
    {
        Calls.Add((data, target));

        return new ModelTrainingResult(
            target,
            AreaUnderRocCurve: 0.8,
            F1Score: 0.5,
            TrainingRowCount: data.Train.Count,
            ModelPath: $"models/{target}.zip",
            TrainerName: "FakeTrainer",
            ValidationRowCount: data.Validation.Count,
            LogLoss: 0.6,
            AreaUnderPrecisionRecallCurve: 0.4,
            PositivePrecision: 0.5,
            PositiveRecall: 0.5,
            FeatureNames: ["GridPosition", "QualiGapToPole"]);
    }
}
