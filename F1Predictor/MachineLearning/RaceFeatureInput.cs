using Microsoft.ML.Data;

namespace F1Predictor.MachineLearning;

public class RaceFeatureInput
{
    public float GridPosition { get; set; }
    public float QualiGapToPole { get; set; }
    public float PitStopCount { get; set; }
    public float AvgPitStopDuration { get; set; }
    public float Rainfall { get; set; }

    [ColumnName("Label")]
    public bool Label { get; set; }
}
