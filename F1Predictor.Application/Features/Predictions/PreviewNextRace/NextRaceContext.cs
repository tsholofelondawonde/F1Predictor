using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Domain.Predictions;
using F1Predictor.Domain.RaceData.Entities;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace F1Predictor.Application.Features.Predictions.PreviewNextRace;

internal sealed record NextRaceContext(
    UpcomingRace Race,
    IReadOnlyList<DriverEntry> Entries,
    bool GridConfirmed,
    IReadOnlyDictionary<int, DriverRaceFeature> Features,
    IReadOnlyDictionary<int, DriverSeasonForm> History)
{
    /// <summary>
    /// Everything a next-race use case needs, loaded once: the race, its entry list, and one
    /// feature row per driver — from the real grid where there is one, projected from form
    /// where there is not. Shared by the preview, the explanation and the generated narrative
    /// so the projection rule is defined exactly once.
    /// </summary>
    public static async Task<Result<NextRaceContext>> LoadAsync(
        IApplicationDbContext context,
        int year,
        CancellationToken cancellationToken)
    {
        var race = await NextRaceAsync(context, year, cancellationToken);

        if (race is null)
        {
            return Result.Failure<NextRaceContext>(PredictionErrors.NoUpcomingRace(year));
        }

        var entries = await context.DriverEntries
            .Where(d => d.SessionKey == race.SessionKey)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (entries.Count == 0)
        {
            return Result.Failure<NextRaceContext>(PredictionErrors.NoEntryList(race.MeetingName));
        }

        var grid = await context.StartingGridEntries
            .Where(g => g.SessionKey == race.SessionKey)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var history = await SeasonFormAsync(context, year, cancellationToken);

        var features = grid.Count > 0
            ? StartingGridProjection.FromActualGrid(entries, grid, history)
            : StartingGridProjection.FromRecentForm(entries, history);

        return Result.Success(new NextRaceContext(race, entries, grid.Count > 0, features, history));
    }

    /// <summary>
    /// The next Grand Prix without results. Sprints are excluded: the models were fitted on races
    /// only, and a sprint's shorter distance and different points scale make them a poor fit for
    /// a podium-or-not question anyway.
    /// </summary>
    /// <remarks>
    /// The date filter is not redundant with the results check. OpenF1 has rounds it never
    /// published a result for, which stay unclassified for ever; without it, the "next" race
    /// could be one that was run months ago.
    /// </remarks>
    private static async Task<UpcomingRace?> NextRaceAsync(
        IApplicationDbContext context,
        int year,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        return await (
            from session in context.RaceSessions
            join meeting in context.Meetings on session.MeetingKey equals meeting.MeetingKey
            where meeting.Year == year
                && !session.IsSprint
                && !session.IsClassified
                && session.DateStart > now
            orderby session.DateStart
            select new UpcomingRace(
                session.SessionKey,
                meeting.MeetingKey,
                meeting.MeetingName,
                meeting.CircuitShortName,
                meeting.CountryName,
                session.DateStart,
                context.RaceSessions
                    .Where(s => s.MeetingKey == meeting.MeetingKey && s.IsSprint)
                    .Select(s => (int?)s.SessionKey)
                    .FirstOrDefault(),
                context.RaceSessions
                    .Where(s => s.MeetingKey == meeting.MeetingKey && s.IsSprint)
                    .Select(s => (DateTimeOffset?)s.DateStart)
                    .FirstOrDefault()))
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Each driver's season so far, averaged from the feature rows the models were trained on —
    /// which already exclude sprints and unclassified races.
    /// </summary>
    private static async Task<Dictionary<int, DriverSeasonForm>> SeasonFormAsync(
        IApplicationDbContext context,
        int year,
        CancellationToken cancellationToken)
    {
        var features = await (
            from feature in context.DriverRaceFeatures
            join session in context.RaceSessions on feature.SessionKey equals session.SessionKey
            join meeting in context.Meetings on session.MeetingKey equals meeting.MeetingKey
            where meeting.Year == year
            select feature)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return features
            .GroupBy(f => f.DriverNumber)
            .ToDictionary(group => group.Key, DriverSeasonForm.From);
    }
}

internal sealed record UpcomingRace(
    int SessionKey,
    int MeetingKey,
    string MeetingName,
    string CircuitShortName,
    string CountryName,
    DateTimeOffset DateStart,
    int? SprintSessionKey,
    DateTimeOffset? SprintDateStart);
