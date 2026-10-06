namespace F1Predictor.Domain.Predictions;

/// <summary>
/// One classifier fitted by one training run, with the numbers it scored. Append-only: every
/// run adds a row per target and nothing is ever overwritten, so the table is the experiment
/// log — what changed (<see cref="Notes"/>, <see cref="FeatureNames"/>, the season window) next
/// to what it did to the metrics.
/// </summary>
/// <remarks>
/// The model files themselves are still overwritten on every run; this is the record of how
/// each one scored, not a model registry.
/// </remarks>
public class ModelTrainingRun
{
    public const int TargetMaxLength = 32;
    public const int TrainerNameMaxLength = 100;
    public const int FeatureNamesMaxLength = 2000;
    public const int NotesMaxLength = 500;

    public int Id { get; set; }

    public DateTimeOffset TrainedAt { get; set; }

    /// <summary>"Podium" or "PointsFinish" — the prediction target's name.</summary>
    public string Target { get; set; } = "";

    /// <summary>First season in the training window.</summary>
    public int FromYear { get; set; }

    /// <summary>Last season in the training window; the holdout race is from this season.</summary>
    public int Year { get; set; }

    /// <summary>The trainer AutoML picked, e.g. "LightGbmBinary".</summary>
    public string TrainerName { get; set; } = "";

    /// <summary>Comma-separated feature columns, in model order.</summary>
    public string FeatureNames { get; set; } = "";

    public int TrainingRaceCount { get; set; }
    public int TrainingRowCount { get; set; }
    public int ValidationRaceCount { get; set; }
    public int ValidationRowCount { get; set; }

    public double ValidationAuc { get; set; }
    public double ValidationF1 { get; set; }

    /// <summary>In bits: 1.0 is a coin flip, lower is better.</summary>
    public double ValidationLogLoss { get; set; }

    public double ValidationAuprc { get; set; }
    public double ValidationPrecision { get; set; }
    public double ValidationRecall { get; set; }

    /// <summary>
    /// AUC of simply ranking the validation rows by grid position. The model has to beat this
    /// to have learned anything the starting grid didn't already say. Null when undefined.
    /// </summary>
    public double? BaselineAuc { get; set; }

    /// <summary>The race held out of training entirely.</summary>
    public int HoldoutSessionKey { get; set; }

    /// <summary>AUC on the held-out race, scored through the saved model. Noisy (~20 rows); null when undefined.</summary>
    public double? HoldoutAuc { get; set; }

    /// <summary>Log loss in bits on the held-out race. Null when there were no rows.</summary>
    public double? HoldoutLogLoss { get; set; }

    /// <summary>Free text describing the experiment, e.g. "removed pit-stop features".</summary>
    public string? Notes { get; set; }
}
