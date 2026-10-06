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

    public static RaceFeatureInput From(DriverRaceFeature feature, bool label = false) => new()
    {
        GridPosition = feature.GridPosition,
        QualiGapToPole = feature.QualiGapToPole,
        PitStopCount = feature.PitStopCount,
        AvgPitStopDuration = feature.AvgPitStopDuration,
        Rainfall = feature.Rainfall,
        Label = label
    };

    /// <summary>
    /// Feature columns in declaration order — the order AutoML concatenates them into
    /// <c>Features</c>, and the fallback when a saved model carries no slot names.
    /// </summary>
    public static readonly string[] FeatureNames =
    [
        nameof(GridPosition), nameof(QualiGapToPole), nameof(PitStopCount), nameof(AvgPitStopDuration), nameof(Rainfall)
    ];

    /// <summary>Raw value of each feature, by the same names, for reporting alongside a contribution.</summary>
    public static Dictionary<string, float> ValuesByName(DriverRaceFeature feature) => new(StringComparer.Ordinal)
    {
        [nameof(GridPosition)] = feature.GridPosition,
        [nameof(QualiGapToPole)] = feature.QualiGapToPole,
        [nameof(PitStopCount)] = feature.PitStopCount,
        [nameof(AvgPitStopDuration)] = feature.AvgPitStopDuration,
        [nameof(Rainfall)] = feature.Rainfall
    };
}
