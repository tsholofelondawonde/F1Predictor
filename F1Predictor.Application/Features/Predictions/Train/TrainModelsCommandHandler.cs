using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Abstractions.MachineLearning;
using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Predictions.Evaluation;
using F1Predictor.Domain.Predictions;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace F1Predictor.Application.Features.Predictions.Train;

internal sealed class TrainModelsCommandHandler(
    IApplicationDbContext context,
    IModelTrainer trainer,
    IRacePredictor predictor,
    IDateTimeProvider dateTimeProvider,
    ILogger<TrainModelsCommandHandler> logger)
    : ICommandHandler<TrainModelsCommand, TrainModelsResponse>
{
    /// <summary>One holdout race plus the fewest races a race-grouped time split can use.</summary>
    internal const int MinimumRacesToTrain = RaceTimeSplit.MinimumRaces + 1;

    private const string MetricGuidance =
        "Metrics are measured on the latest races before the holdout, which the model did not " +
        "train on. Compare validation AUC against BaselineAuc: ranking by grid position alone. " +
        "A model that can't beat it has learned nothing the grid didn't already say. Judge by " +
        "AUC, log loss (in bits; 1.0 is a coin flip) and F1, never accuracy: podium is about 15% " +
        "of rows, so always answering \"no\" scores ~85%. Holdout numbers come from a single race " +
        "and swing a lot. Use GET /api/models/runs to compare this run with earlier ones.";

    public async Task<Result<TrainModelsResponse>> Handle(
        TrainModelsCommand command,
        CancellationToken cancellationToken)
    {
        var season = await SeasonFeatureSet.LoadAsync(context, command.Year, cancellationToken, command.FromYear);

        if (season.Holdout is not { } holdout || season.RaceCount < MinimumRacesToTrain)
        {
            return InsufficientData(season);
        }

        var split = RaceTimeSplit.Split(season.Features, season.TrainingRaces);

        logger.LogInformation(
            "Training on {TrainRaces} races, validating on {ValidationRaces} ({FromYear}-{Year}). Holding out {HoldoutRace}.",
            split.TrainingRaces.Count, split.ValidationRaces.Count, season.FromYear, season.Year, holdout.MeetingName);

        var podium = trainer.Train(split.Data, PredictionTarget.Podium);
        var points = trainer.Train(split.Data, PredictionTarget.PointsFinish);

        // Scored through IRacePredictor, i.e. the files just saved — which checks the shipped
        // artefact end to end, not just an in-memory model.
        var run = new RunContext(
            season,
            split,
            [.. season.HoldoutFeatures.Select(f => (f, predictor.Predict(f)))],
            new DateTimeOffset(dateTimeProvider.UtcNow, TimeSpan.Zero),
            string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim());

        var podiumRun = run.Record(podium, f => f.Podium, p => p.PodiumProbability);
        var pointsRun = run.Record(points, f => f.PointsFinish, p => p.PointsProbability);

        context.ModelTrainingRuns.AddRange(podiumRun, pointsRun);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Podium model: {PodiumTrainer}, AUC {PodiumAuc:F3} (grid baseline {PodiumBaseline:F3}); " +
            "points model: {PointsTrainer}, AUC {PointsAuc:F3} (grid baseline {PointsBaseline:F3}).",
            podium.TrainerName, podium.AreaUnderRocCurve, podiumRun.BaselineAuc,
            points.TrainerName, points.AreaUnderRocCurve, pointsRun.BaselineAuc);

        return Result.Success(new TrainModelsResponse(
            season.Year,
            season.TrainingRaces.Count,
            split.Data.Train.Count + split.Data.Validation.Count,
            holdout.MeetingName,
            podium,
            points,
            MetricGuidance,
            season.FromYear,
            [.. split.ValidationRaces.Select(r => r.MeetingName)],
            EvaluationOf(podiumRun),
            EvaluationOf(pointsRun)));
    }

    /// <summary>
    /// The no-model baseline: rank by starting slot, front of the grid first. Negated because a
    /// lower grid number should score higher.
    /// </summary>
    internal static double? GridBaselineAuc(IReadOnlyList<DriverRaceFeature> rows, Func<DriverRaceFeature, bool> label) =>
        BinaryMetrics.Auc([.. rows.Select(f => (-(double)f.GridPosition, label(f)))]);

    private Result<TrainModelsResponse> InsufficientData(SeasonFeatureSet season)
    {
        logger.LogWarning(
            "Only {RaceCount} race(s) have usable feature data for {FromYear}-{Year}.",
            season.RaceCount, season.FromYear, season.Year);

        var noneInYear = season.Holdout is null ? $", none of them in {season.Year}" : "";

        return Result.Failure<TrainModelsResponse>(Error.Problem(
            "Training.InsufficientData",
            $"Only {season.RaceCount} race(s) have usable feature data for {season.FromYear}-{season.Year}" +
            $"{noneInYear} - need at least {MinimumRacesToTrain}, one of them in {season.Year}, to train.",
            "There is not enough race data for that season yet. Ingest a completed season, " +
            "or rebuild the features first."));
    }

    private static TargetEvaluation EvaluationOf(ModelTrainingRun run) =>
        new(run.Id, run.BaselineAuc, run.HoldoutAuc, run.HoldoutLogLoss);

    /// <summary>Everything one run's two <see cref="ModelTrainingRun"/> rows share.</summary>
    private sealed record RunContext(
        SeasonFeatureSet Season,
        RaceSplit Split,
        IReadOnlyList<(DriverRaceFeature Feature, RaceProbabilities Probabilities)> HoldoutScores,
        DateTimeOffset TrainedAt,
        string? Notes)
    {
        public ModelTrainingRun Record(
            ModelTrainingResult result,
            Func<DriverRaceFeature, bool> label,
            Func<RaceProbabilities, float> probability)
        {
            List<(double, bool)> holdout = [.. HoldoutScores.Select(s => ((double)probability(s.Probabilities), label(s.Feature)))];

            return new ModelTrainingRun
            {
                TrainedAt = TrainedAt,
                Target = result.Target.ToString(),
                FromYear = Season.FromYear,
                Year = Season.Year,
                TrainerName = Truncate(result.TrainerName, ModelTrainingRun.TrainerNameMaxLength),
                FeatureNames = Truncate(string.Join(',', result.FeatureNames), ModelTrainingRun.FeatureNamesMaxLength),
                TrainingRaceCount = Split.TrainingRaces.Count,
                TrainingRowCount = Split.Data.Train.Count,
                ValidationRaceCount = Split.ValidationRaces.Count,
                ValidationRowCount = Split.Data.Validation.Count,
                ValidationAuc = result.AreaUnderRocCurve,
                ValidationF1 = result.F1Score,
                ValidationLogLoss = result.LogLoss,
                ValidationAuprc = result.AreaUnderPrecisionRecallCurve,
                ValidationPrecision = result.PositivePrecision,
                ValidationRecall = result.PositiveRecall,
                BaselineAuc = GridBaselineAuc(Split.Data.Validation, label),
                HoldoutSessionKey = Season.Holdout!.SessionKey,
                HoldoutAuc = BinaryMetrics.Auc(holdout),
                HoldoutLogLoss = BinaryMetrics.LogLoss(holdout),
                Notes = Notes
            };
        }

        private static string Truncate(string value, int maxLength) =>
            value.Length <= maxLength ? value : value[..maxLength];
    }
}
