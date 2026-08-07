using Microsoft.ML.Data;

namespace F1Predictor.MachineLearning;

public class RacePredictionOutput
{
    [ColumnName("PredictedLabel")]
    public bool PredictedLabel { get; set; }

    public float Probability { get; set; }

    public float Score { get; set; }
}
