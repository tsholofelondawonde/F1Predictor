using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Abstractions.MachineLearning;
using F1Predictor.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace F1Predictor.Application.Features.Seasons.GetDataStatus;

internal sealed class GetDataStatusQueryHandler(IApplicationDbContext context, IRacePredictor predictor)
    : IQueryHandler<GetDataStatusQuery, DataStatusResponse>
{
    public async Task<Result<DataStatusResponse>> Handle(
        GetDataStatusQuery query,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var pending = await (
            from session in context.RaceSessions
            join meeting in context.Meetings on session.MeetingKey equals meeting.MeetingKey
            where meeting.Year == query.Year
                && !session.IsSprint
                && !session.IsClassified
                && session.DateStart < now
            orderby session.DateStart
            select new { meeting.MeetingName, session.DateStart })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        var featuresStale = await context.RaceSessions
            .Join(context.Meetings, session => session.MeetingKey, meeting => meeting.MeetingKey, (session, meeting) => new { session, meeting })
            .Where(x => x.meeting.Year == query.Year && !x.session.IsSprint && x.session.IsClassified)
            .AnyAsync(x => !context.DriverRaceFeatures.Any(f => f.SessionKey == x.session.SessionKey), cancellationToken);

        var modelsAvailable = predictor.ModelsAvailable;

        var isStale = pending is not null || featuresStale || !modelsAvailable;

        return Result.Success(new DataStatusResponse(
            isStale,
            pending?.MeetingName,
            pending?.DateStart,
            featuresStale,
            modelsAvailable));
    }
}
