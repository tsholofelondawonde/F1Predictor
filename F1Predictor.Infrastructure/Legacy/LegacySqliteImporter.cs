using F1Predictor.Application.Abstractions.Legacy;
using F1Predictor.Domain.Predictions;
using F1Predictor.Domain.RaceData.Entities;
using F1Predictor.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace F1Predictor.Infrastructure.Legacy;

/// <summary>
/// Copies the console prototype's SQLite data into the application database.
/// </summary>
/// <remarks>
/// Existing race sessions in the target are left untouched and their child rows skipped, so
/// the import is idempotent. Surrogate <c>Id</c> values are not carried across — they are
/// database-generated and mean nothing outside their own file. The natural OpenF1 keys
/// (<c>MeetingKey</c>, <c>SessionKey</c>) are preserved, and those are what everything joins on.
/// </remarks>
internal sealed class LegacySqliteImporter(ApplicationDbContext target) : ILegacyDatabaseImporter
{
    public async Task<LegacyImportCounts> ImportAsync(string sqlitePath, CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<LegacySqliteContext>()
            .UseSqlite($"Data Source={sqlitePath};Mode=ReadOnly")
            .Options;

        await using var source = new LegacySqliteContext(options);

        var existingMeetingKeys = (await target.Meetings
            .Select(m => m.MeetingKey)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var existingSessionKeys = (await target.RaceSessions
            .Select(s => s.SessionKey)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var meetings = await source.Meetings
            .Where(m => !existingMeetingKeys.Contains(m.MeetingKey))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var sessions = await source.RaceSessions
            .Where(s => !existingSessionKeys.Contains(s.SessionKey))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var newSessionKeys = sessions.Select(s => s.SessionKey).ToHashSet();
        var skippedSessions = await source.RaceSessions
            .CountAsync(s => existingSessionKeys.Contains(s.SessionKey), cancellationToken);

        var grid = await CopyChildrenAsync(source.StartingGridEntries, newSessionKeys, g => g.SessionKey, cancellationToken);
        var results = await CopyChildrenAsync(source.SessionResultEntries, newSessionKeys, r => r.SessionKey, cancellationToken);
        var pits = await CopyChildrenAsync(source.PitStopEntries, newSessionKeys, p => p.SessionKey, cancellationToken);
        var weather = await CopyChildrenAsync(source.WeatherReadings, newSessionKeys, w => w.SessionKey, cancellationToken);
        var features = await CopyChildrenAsync(source.DriverRaceFeatures, newSessionKeys, f => f.SessionKey, cancellationToken);

        await StageAndSaveAsync(meetings, sessions, grid, results, pits, weather, features, cancellationToken);

        return new LegacyImportCounts(
            meetings.Count,
            sessions.Count,
            grid.Count,
            results.Count,
            pits.Count,
            weather.Count,
            features.Count,
            skippedSessions);
    }

    /// <summary>
    /// Attaches the copied rows and writes them in one transaction. Surrogate <c>Id</c>s are
    /// cleared first so Postgres assigns its own rather than colliding with whatever SQLite
    /// happened to number them.
    /// </summary>
    private async Task StageAndSaveAsync(
        List<Meeting> meetings,
        List<RaceSession> sessions,
        List<StartingGridEntry> grid,
        List<SessionResultEntry> results,
        List<PitStopEntry> pits,
        List<WeatherReading> weather,
        List<DriverRaceFeature> features,
        CancellationToken cancellationToken)
    {
        // The prototype only ever stored completed Grands Prix, so every imported session is a
        // classified non-sprint. It never captured points, though, which is why imported rows
        // score zero until the season is re-ingested with force.
        sessions.ForEach(s =>
        {
            s.IsSprint = false;
            s.IsClassified = true;
        });

        grid.ForEach(g => g.Id = 0);
        results.ForEach(r => r.Id = 0);
        pits.ForEach(p => p.Id = 0);
        weather.ForEach(w => w.Id = 0);
        features.ForEach(f => f.Id = 0);

        target.Meetings.AddRange(meetings);
        target.RaceSessions.AddRange(sessions);
        target.StartingGridEntries.AddRange(grid);
        target.SessionResultEntries.AddRange(results);
        target.PitStopEntries.AddRange(pits);
        target.WeatherReadings.AddRange(weather);
        target.DriverRaceFeatures.AddRange(features);

        await target.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Pulls a whole child table out of SQLite and keeps only the rows belonging to sessions
    /// being imported. The filtering happens in memory because the legacy file holds a single
    /// season — a few thousand rows in total.
    /// </summary>
    private static async Task<List<T>> CopyChildrenAsync<T>(
        DbSet<T> sourceSet,
        HashSet<int> sessionKeys,
        Func<T, int> sessionKeySelector,
        CancellationToken cancellationToken)
        where T : class
    {
        var rows = await sourceSet.AsNoTracking().ToListAsync(cancellationToken);

        return rows.Where(row => sessionKeys.Contains(sessionKeySelector(row))).ToList();
    }
}
