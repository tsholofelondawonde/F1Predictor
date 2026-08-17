using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace F1Predictor.Application.Features.Seasons.GetRaces;

internal sealed class GetSeasonRacesQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetSeasonRacesQuery, IReadOnlyList<SeasonRaceResponse>>
{
    public async Task<Result<IReadOnlyList<SeasonRaceResponse>>> Handle(
        GetSeasonRacesQuery query,
        CancellationToken cancellationToken)
    {
        var races = await (
            from session in context.RaceSessions
            join meeting in context.Meetings on session.MeetingKey equals meeting.MeetingKey
            where meeting.Year == query.Year
            orderby session.DateStart
            select new SeasonRaceResponse(
                session.SessionKey,
                meeting.MeetingName,
                meeting.CircuitShortName,
                meeting.CountryName,
                session.DateStart,
                session.IsSprint,
                session.IsClassified,
                context.DriverRaceFeatures.Count(f => f.SessionKey == session.SessionKey)))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<SeasonRaceResponse>>(races);
    }
}
