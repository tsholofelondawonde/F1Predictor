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

        var drivers = season.HoldoutFeatures
            .Select(feature =>
            {
                var probabilities = predictor.Predict(feature);
                return new DriverPrediction(
                    feature.DriverNumber,
                    feature.GridPosition,
                    feature.FinishPosition,
                    feature.Podium,
                    feature.PointsFinish,
                    probabilities.PodiumProbability,
                    probabilities.PointsProbability);
            })
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
