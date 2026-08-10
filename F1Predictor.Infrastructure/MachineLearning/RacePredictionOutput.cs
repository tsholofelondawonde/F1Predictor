using Microsoft.ML.Data;

namespace F1Predictor.Infrastructure.MachineLearning;

/// <summary>
/// What a trained binary classifier returns for one row.
/// </summary>
internal sealed class RacePredictionOutput
{
    [ColumnName("PredictedLabel")]
    public bool PredictedLabel { get; set; }

    /// <summary>Calibrated probability of the positive class — the number worth showing a user.</summary>
    public float Probability { get; set; }

    /// <summary>Raw uncalibrated score. Useful for ranking, not for reading as a percentage.</summary>
    public float Score { get; set; }
}
