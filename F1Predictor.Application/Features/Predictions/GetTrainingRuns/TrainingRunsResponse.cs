namespace F1Predictor.Application.Features.Predictions.GetTrainingRuns;

/// <param name="Runs">Newest first.</param>
public sealed record TrainingRunsResponse(IReadOnlyList<TrainingRunResponse> Runs);

/// <summary>One recorded classifier. See <c>ModelTrainingRun</c> for what each number means.</summary>
/// <param name="AucOverBaseline">
/// Validation AUC minus the grid-order baseline — the single number that says whether the
/// model knows anything the grid doesn't. Null when the baseline was undefined.
/// </param>
public sealed record TrainingRunResponse(
    int Id,
    DateTimeOffset TrainedAt,
    string Target,
    int FromYear,
    int Year,
    string TrainerName,
    IReadOnlyList<string> FeatureNames,
    int TrainingRaceCount,
    int TrainingRowCount,
    int ValidationRaceCount,
    int ValidationRowCount,
    double ValidationAuc,
    double ValidationF1,
    double ValidationLogLoss,
    double ValidationAuprc,
    double ValidationPrecision,
    double ValidationRecall,
    double? BaselineAuc,
    double? AucOverBaseline,
    int HoldoutSessionKey,
    double? HoldoutAuc,
    double? HoldoutLogLoss,
    string? Notes);
