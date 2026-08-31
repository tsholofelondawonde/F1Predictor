using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Abstractions.MachineLearning;
using F1Predictor.Application.Abstractions.Messaging;
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
            drivers);

        return Result.Success(response);
    }
}
