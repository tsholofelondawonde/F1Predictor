using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Abstractions.MachineLearning;
using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Predictions.GetHoldout;
using Microsoft.EntityFrameworkCore;
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

        var features = await context.DriverRaceFeatures
            .Where(f => f.SessionKey == query.SessionKey)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (features.Count == 0)
        {
            return Result.Failure<RacePredictionsResponse>(PredictionErrors.RaceNotFound(query.SessionKey));
        }

        var meetingName = await (
            from session in context.RaceSessions
            join meeting in context.Meetings on session.MeetingKey equals meeting.MeetingKey
            where session.SessionKey == query.SessionKey
            select meeting.MeetingName)
            .FirstOrDefaultAsync(cancellationToken) ?? "Unknown meeting";

        var directory = await DriverDirectory.ForSessionAsync(context, query.SessionKey, cancellationToken);

        var drivers = features
            .Select(feature => directory.Describe(feature, predictor.Predict(feature)))
            .OrderByDescending(d => d.PodiumProbability)
            .ToList();

        return Result.Success(new RacePredictionsResponse(query.SessionKey, meetingName, drivers));
    }
}
