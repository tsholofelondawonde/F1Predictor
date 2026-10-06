using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Abstractions.MachineLearning;
using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Predictions.GetHoldout;
using SharedKernel;

namespace F1Predictor.Application.Features.Predictions.PredictRace;

internal sealed class PredictRaceQueryHandler(
    IApplicationDbContext context,
    IRacePredictor predictor)
    : IQueryHandler<PredictRaceQuery, RacePredictionsResponse>
{
    public async Task<Result<RacePredictionsResponse>> Handle(
        PredictRaceQuery query,
        CancellationToken cancellationToken)
    {
        if (!predictor.ModelsAvailable)
        {
            return Result.Failure<RacePredictionsResponse>(PredictionErrors.ModelsNotTrained);
        }

        var loaded = await ClassifiedRace.LoadAsync(context, query.SessionKey, cancellationToken);
        if (loaded.IsFailure)
        {
            return Result.Failure<RacePredictionsResponse>(loaded.Error);
        }

        var race = loaded.Value;
        var directory = new DriverDirectory(race.Directory);

        var drivers = race.Features.Values
            .Select(feature => directory.Describe(feature, predictor.Predict(feature)))
            .OrderByDescending(d => d.PodiumProbability)
            .ToList();

        return Result.Success(new RacePredictionsResponse(query.SessionKey, race.MeetingName, drivers));
    }
}
