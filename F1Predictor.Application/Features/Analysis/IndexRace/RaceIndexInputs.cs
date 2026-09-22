using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Domain.RaceData.Entities;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace F1Predictor.Application.Features.Analysis.IndexRace;

/// <summary>
/// The six raw inputs <c>RaceFactSheet.Build</c> needs for one session, loaded once — same
/// loader-first shape as <c>ClassifiedRace.LoadAsync</c> and <c>NextRaceContext.LoadAsync</c>.
/// No existing loader already combines this exact set: <c>RebuildFeaturesCommandHandler</c>
/// loads the same four raw tables (grid, results, pit stops, weather) but across every session
/// at once for feature engineering, and <c>ClassifiedRace</c>/<c>NextRaceContext</c> load
/// already-derived <c>DriverRaceFeature</c> rows rather than the raw rows a fact sheet needs.
/// </summary>
internal sealed record RaceIndexInputs(
    Meeting Meeting,
    RaceSession Session,
    IReadOnlyList<SessionResultEntry> Results,
    IReadOnlyList<StartingGridEntry> Grid,
    IReadOnlyList<PitStopEntry> PitStops,
    IReadOnlyList<WeatherReading> Weather,
    IReadOnlyDictionary<int, DriverEntry> Directory)
{
    public static async Task<Result<RaceIndexInputs>> LoadAsync(
        IApplicationDbContext context,
        int sessionKey,
        CancellationToken cancellationToken)
    {
        var loaded = await (
            from session in context.RaceSessions
            join meeting in context.Meetings on session.MeetingKey equals meeting.MeetingKey
            where session.SessionKey == sessionKey
            select new { session, meeting })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (loaded is null)
        {
            return Result.Failure<RaceIndexInputs>(AnalysisErrors.RaceNotFound(sessionKey));
        }

        var results = await context.SessionResultEntries
            .Where(r => r.SessionKey == sessionKey)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var grid = await context.StartingGridEntries
            .Where(g => g.SessionKey == sessionKey)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var pitStops = await context.PitStopEntries
            .Where(p => p.SessionKey == sessionKey)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var weather = await context.WeatherReadings
            .Where(w => w.SessionKey == sessionKey)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var directory = await context.DriverEntries
            .Where(d => d.SessionKey == sessionKey)
            .AsNoTracking()
            .ToDictionaryAsync(d => d.DriverNumber, cancellationToken);

        return Result.Success(new RaceIndexInputs(loaded.meeting, loaded.session, results, grid, pitStops, weather, directory));
    }
}
