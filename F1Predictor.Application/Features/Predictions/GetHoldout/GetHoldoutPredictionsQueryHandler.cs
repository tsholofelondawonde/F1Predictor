using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Abstractions.MachineLearning;
using F1Predictor.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace F1Predictor.Application.Features.Predictions.GetHoldout;

internal sealed class GetHoldoutPredictionsQueryHandler(
    IApplicationDbContext context,
    IRacePredictor predictor)
    : IQueryHandler<GetHoldoutPredictionsQuery, HoldoutPredictionsResponse>
{
    public async Task<Result<HoldoutPredictionsResponse>> Handle(
        GetHoldoutPredictionsQuery query,
        CancellationToken cancellationToken)
    {
        if (!predictor.ModelsAvailable)
        {
            return Result.Failure<HoldoutPredictionsResponse>(PredictionErrors.ModelsNotTrained);
        }

        var season = await SeasonFeatureSet.LoadAsync(context, query.Year, cancellationToken);

        if (season.Holdout is null)
        {
            return Result.Failure<HoldoutPredictionsResponse>(Error.NotFound(
                "Holdout.NoRaces",
                $"No races with usable feature data were found for {query.Year}.",
                "There is no race data for that season yet."));
        }

        var directory = await DriverDirectory.ForSessionAsync(
            context, season.Holdout.SessionKey, cancellationToken);

        var drivers = season.HoldoutFeatures
            .Select(feature => directory.Describe(feature, predictor.Predict(feature)))
            .ToList();

        var response = new HoldoutPredictionsResponse(
            query.Year,
            season.Holdout.SessionKey,
            season.Holdout.MeetingName,
            season.Holdout.CircuitShortName,
            season.Holdout.DateStart,
            drivers,
            await ModelWarningAsync(season.Holdout, cancellationToken));

        return Result.Success(response);
    }

    /// <summary>
    /// Both targets are trained and recorded together with the same holdout, so the newest run
    /// row speaks for the models on disk.
    /// </summary>
    private async Task<string?> ModelWarningAsync(SeasonRace holdout, CancellationToken cancellationToken)
    {
        var latest = await context.ModelTrainingRuns
            .AsNoTracking()
            .OrderByDescending(r => r.TrainedAt)
            .ThenByDescending(r => r.Id)
            .Select(r => new { r.HoldoutSessionKey, r.FromYear, r.Year })
            .FirstOrDefaultAsync(cancellationToken);

        if (latest is null)
        {
            return "No training run is recorded, so it can't be confirmed these models held this " +
                "race out. Retrain to be sure the predictions below are an honest test.";
        }

        if (latest.HoldoutSessionKey != holdout.SessionKey)
        {
            var window = latest.FromYear == latest.Year ? $"{latest.Year}" : $"{latest.FromYear}-{latest.Year}";

            return $"The current models were last trained on {window} with a different race held out, " +
                $"so {holdout.MeetingName} may be in their training data. Retrain with year={holdout.Year} " +
                "for an honest test.";
        }

        return null;
    }
}
