using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Abstractions.MachineLearning;
using F1Predictor.Application.Abstractions.Messaging;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace F1Predictor.Application.Features.Predictions.Train;

internal sealed class TrainModelsCommandHandler(
    IApplicationDbContext context,
    IModelTrainer trainer,
    ILogger<TrainModelsCommandHandler> logger)
    : ICommandHandler<TrainModelsCommand, TrainModelsResponse>
{
    /// <summary>
    /// Below this there is not enough signal to fit anything worth looking at, and the
    /// train/test split stops being meaningful.
    /// </summary>
    private const int MinimumRacesToTrain = 3;

    private const string MetricGuidance =
        "Judge these models by AUC and F1, not accuracy. Podium is roughly 15% of rows, so a " +
        "model that always answers \"no\" already scores about 85% accuracy while being useless. " +
        "AUC near 0.5 is noise; grid position alone should get comfortably above 0.65.";

    public async Task<Result<TrainModelsResponse>> Handle(
        TrainModelsCommand command,
        CancellationToken cancellationToken)
    {
        var season = await SeasonFeatureSet.LoadAsync(context, command.Year, cancellationToken);

        if (season.RaceCount < MinimumRacesToTrain)
        {
            logger.LogWarning(
                "Only {RaceCount} race(s) have usable feature data for {Year}.",
                season.RaceCount, command.Year);

            return Result.Failure<TrainModelsResponse>(Error.Problem(
                "Training.InsufficientData",
                $"Only {season.RaceCount} race(s) have usable feature data for {command.Year} - " +
                $"need at least {MinimumRacesToTrain} to train.",
                "There is not enough race data for that season yet. Ingest a completed season, " +
                "or rebuild the features first."));
        }

        var holdout = season.Holdout!;
        var trainingFeatures = season.TrainingFeatures;

        logger.LogInformation(
            "Training on {RaceCount} races ({RowCount} driver-race rows). Holding out {HoldoutRace}.",
            season.RaceCount - 1, trainingFeatures.Count, holdout.MeetingName);

        var podium = trainer.Train(trainingFeatures, PredictionTarget.Podium);
        var points = trainer.Train(trainingFeatures, PredictionTarget.PointsFinish);

        logger.LogInformation(
            "Podium model AUC {PodiumAuc:F3} / F1 {PodiumF1:F3}; points model AUC {PointsAuc:F3} / F1 {PointsF1:F3}.",
            podium.AreaUnderRocCurve, podium.F1Score, points.AreaUnderRocCurve, points.F1Score);

        var response = new TrainModelsResponse(
            command.Year,
            season.RaceCount - 1,
            trainingFeatures.Count,
            holdout.MeetingName,
            podium,
            points,
            MetricGuidance);

        return Result.Success(response);
    }
}
