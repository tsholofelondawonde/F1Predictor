using F1Predictor.Domain.Predictions;
using Microsoft.ML.Data;

namespace F1Predictor.Infrastructure.MachineLearning;

/// <summary>
/// The ML.NET view of a feature row. Kept separate from the domain entity because ML.NET
/// binds to public mutable fields by column name and needs a single boolean Label column.
/// </summary>
internal sealed class RaceFeatureInput
{
    public float GridPosition { get; set; }
    public float QualiGapToPole { get; set; }
    public float PitStopCount { get; set; }
    public float AvgPitStopDuration { get; set; }
    public float Rainfall { get; set; }

    /// <summary>
    /// The target being trained: podium or points-finish, depending on which model this row feeds.
    /// </summary>
    [ColumnName("Label")]
    public bool Label { get; set; }

    /// <summary>The five columns concatenated into the "Features" vector, in pipeline order.</summary>
    public static string[] FeatureColumns { get; } =
    [
        nameof(GridPosition),
        nameof(QualiGapToPole),
        nameof(PitStopCount),
        nameof(AvgPitStopDuration),
        nameof(Rainfall)
    ];

    public static RaceFeatureInput From(DriverRaceFeature feature, bool label = false) => new()
    {
        GridPosition = feature.GridPosition,
        QualiGapToPole = feature.QualiGapToPole,
        PitStopCount = feature.PitStopCount,
        AvgPitStopDuration = feature.AvgPitStopDuration,
        Rainfall = feature.Rainfall,
        Label = label
    };
}
