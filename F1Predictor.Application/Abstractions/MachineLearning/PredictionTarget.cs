namespace F1Predictor.Application.Abstractions.MachineLearning;

/// <summary>
/// Which label a model is trained against. Two independent binary classifiers share the
/// same feature set and differ only in this.
/// </summary>
public enum PredictionTarget
{
    /// <summary>Finished in the top three.</summary>
    Podium,

    /// <summary>Finished in the points (top ten).</summary>
    PointsFinish
}
