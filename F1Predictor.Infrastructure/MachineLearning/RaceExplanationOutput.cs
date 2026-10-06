using Microsoft.ML.Data;

namespace F1Predictor.Infrastructure.MachineLearning;

/// <summary>
/// <see cref="RacePredictionOutput"/> plus the per-slot contribution vector the
/// feature-contribution transform appends.
/// </summary>
internal sealed class RaceExplanationOutput
{
    [ColumnName("PredictedLabel")]
    public bool PredictedLabel { get; set; }

    public float Probability { get; set; }

    public float Score { get; set; }

    [ColumnName("FeatureContributions")]
    public float[] FeatureContributions { get; set; } = [];
}
