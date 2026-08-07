using F1Predictor.Application.Abstractions.MachineLearning;

namespace F1Predictor.Infrastructure.MachineLearning;

/// <summary>
/// Where trained models are written and read from. Bound from the "MachineLearning" configuration section.
/// </summary>
internal sealed class ModelStorageOptions
{
    public const string SectionName = "MachineLearning";

    /// <summary>
    /// Directory holding the saved <c>.zip</c> models. Relative paths resolve against the
    /// content root. Defaults to a "models" folder so trained models are not scattered
    /// through the working directory.
    /// </summary>
    public string ModelDirectory { get; set; } = "models";

    public string PathFor(PredictionTarget target) =>
        Path.GetFullPath(Path.Combine(ModelDirectory, FileNameFor(target)));

    private static string FileNameFor(PredictionTarget target) => target switch
    {
        PredictionTarget.Podium => "podium-model.zip",
        PredictionTarget.PointsFinish => "points-model.zip",
        _ => throw new ArgumentOutOfRangeException(nameof(target), target, "Unknown prediction target.")
    };
}
