using System.Globalization;
using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Domain.Championship;
using F1Predictor.Domain.RaceData.Entities;
using Microsoft.EntityFrameworkCore;

namespace F1Predictor.Application.Features.Championship;

/// <summary>
/// A season's championship picture, loaded once: where the two tables stand, how quick and how
/// reliable each driver has been, and what is left to run.
/// </summary>
/// <remarks>
/// Shared by the standings, forecast and scenario use cases so all three agree on the same
/// numbers — the counterpart to <see cref="Predictions.SeasonFeatureSet"/> on the modelling side.
/// </remarks>
internal sealed class SeasonChampionship
{
    private SeasonChampionship(
        int year,
        ChampionshipStandings standings,
        IReadOnlyList<DriverForm> forms,
        IReadOnlyList<RemainingSession> remaining,
        int racesCompleted,
        int sprintsCompleted,
        int latestClassifiedSessionKey)
    {
        Year = year;
        Standings = standings;
        Forms = forms;
        Remaining = remaining;
        RacesCompleted = racesCompleted;
        SprintsCompleted = sprintsCompleted;
        LatestClassifiedSessionKey = latestClassifiedSessionKey;
    }

    public int Year { get; }

    public ChampionshipStandings Standings { get; }

    /// <summary>Fitted pace and reliability, one entry per driver who has finished a Grand Prix.</summary>
    public IReadOnlyList<DriverForm> Forms { get; }

    /// <summary>Sessions still to run, earliest first.</summary>
    public IReadOnlyList<RemainingSession> Remaining { get; }

    public int RacesCompleted { get; }

    public int SprintsCompleted { get; }

    /// <summary>Highest session key with results, which is what makes a cached forecast stale.</summary>
    public int LatestClassifiedSessionKey { get; }

    public bool HasResults => Standings.Drivers.Count > 0;

    public bool SeasonComplete => Remaining.Count == 0;

    /// <summary>
    /// Identifies a forecast for caching. Changes the moment a new result lands or the calendar
    /// shifts, so a cached run can never outlive the data it was built from.
    /// </summary>
    public string ForecastCacheKey(int simulations) => string.Create(
        CultureInfo.InvariantCulture,
        $"championship:{Year}:{LatestClassifiedSessionKey}:{Remaining.Count}:{simulations}");

    public static async Task<SeasonChampionship> LoadAsync(
        IApplicationDbContext context,
        int year,
        CancellationToken cancellationToken)
    {
        var sessions = await (
            from session in context.RaceSessions
            join meeting in context.Meetings on session.MeetingKey equals meeting.MeetingKey
            where meeting.Year == year
            orderby session.DateStart
            select new SeasonSession(
                session.SessionKey,
                session.IsSprint,
                session.IsClassified,
                session.DateStart))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var sessionKeys = sessions.ConvertAll(s => s.SessionKey);

        var results = await context.SessionResultEntries
            .Where(r => sessionKeys.Contains(r.SessionKey))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var entries = await context.DriverEntries
            .Where(d => sessionKeys.Contains(d.SessionKey))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var classified = sessions.Where(s => s.IsClassified).ToList();
        var sprintKeys = sessions.Where(s => s.IsSprint).Select(s => s.SessionKey).ToHashSet();
        var classifiedKeys = classified.Select(s => s.SessionKey).ToHashSet();

        var standings = ChampionshipStandings.Build(
            [.. results.Where(r => classifiedKeys.Contains(r.SessionKey))],
            sprintKeys,
            LatestEntryPerDriver(entries, sessions));

        var forms = DriverFormModel.Fit(BuildRaceOutcomes(classified, results));

        // Still to run means unclassified *and* in the future. OpenF1 has races it never
        // published a result for — the 2026 Bahrain and Saudi Arabian rounds among them — and
        // counting those as remaining would hand every driver points that can never be scored.
        var now = DateTimeOffset.UtcNow;

        var remaining = sessions
            .Where(s => !s.IsClassified && s.DateStart > now)
            .Select(s => new RemainingSession(s.SessionKey, s.IsSprint))
            .ToList();

        return new SeasonChampionship(
            year,
            standings,
            forms,
            remaining,
            classified.Count(s => !s.IsSprint),
            classified.Count(s => s.IsSprint),
            classified.Count > 0 ? classified.Max(s => s.SessionKey) : 0);
    }

    /// <summary>
    /// The most recent entry list a driver appears in, which is who they drive for now. Taking
    /// the latest rather than the first is what makes a mid-season team change land correctly.
    /// </summary>
    private static Dictionary<int, DriverEntry> LatestEntryPerDriver(
        List<DriverEntry> entries,
        List<SeasonSession> sessions)
    {
        var order = sessions
            .Select((session, index) => (session.SessionKey, index))
            .ToDictionary(pair => pair.SessionKey, pair => pair.index);

        return entries
            .GroupBy(e => e.DriverNumber)
            .ToDictionary(
                group => group.Key,
                group => group.MaxBy(e => order.GetValueOrDefault(e.SessionKey, -1))!);
    }

    /// <summary>
    /// Turns results into the finishing orders the strength model reads.
    /// </summary>
    /// <remarks>
    /// Grands Prix only. A sprint runs on the same weekend, in the same car, at the same circuit
    /// as the race beside it, so its order carries almost no information the race does not — and
    /// counting both would weight a sprint weekend twice for no gain in evidence.
    /// <para>
    /// Retirements are handed over separately rather than as a last place: a driver who was
    /// leading when the engine let go was not the slowest car on track, and recording them as
    /// such would drag their fitted pace down for something the reliability model already covers.
    /// </para>
    /// </remarks>
    private static List<RaceOutcome> BuildRaceOutcomes(
        List<SeasonSession> classified,
        List<SessionResultEntry> results)
    {
        var bySession = results.ToLookup(r => r.SessionKey);

        return
        [
            .. classified
                .Where(session => !session.IsSprint)
                .Select(session =>
                {
                    var rows = bySession[session.SessionKey].Where(r => !r.Dns).ToList();

                    return new RaceOutcome(
                        session.SessionKey,
                        [.. rows.Where(r => !r.Dnf && r.Position.HasValue)
                               .OrderBy(r => r.Position!.Value)
                               .Select(r => r.DriverNumber)],
                        [.. rows.Where(r => r.Dnf).Select(r => r.DriverNumber)]);
                })
                .Where(outcome => outcome.Finishers.Count > 0)
        ];
    }

    private sealed record SeasonSession(
        int SessionKey,
        bool IsSprint,
        bool IsClassified,
        DateTimeOffset DateStart);
}
