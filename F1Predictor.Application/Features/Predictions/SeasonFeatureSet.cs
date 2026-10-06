using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Domain.Predictions;
using Microsoft.EntityFrameworkCore;

namespace F1Predictor.Application.Features.Predictions;

/// <summary>
/// Feature rows for a window of seasons plus the race metadata needed to split them, loaded
/// once. Shared by training and holdout reporting so both agree on which race is held out.
/// </summary>
internal sealed class SeasonFeatureSet
{
    private SeasonFeatureSet(int fromYear, int year, IReadOnlyList<DriverRaceFeature> features, IReadOnlyList<SeasonRace> races)
    {
        FromYear = fromYear;
        Year = year;
        Features = features;
        Races = races;
    }

    /// <summary>First season in the window.</summary>
    public int FromYear { get; }

    /// <summary>Last season in the window — the one the holdout comes from.</summary>
    public int Year { get; }

    public IReadOnlyList<DriverRaceFeature> Features { get; }

    /// <summary>Races that produced feature rows, across every season in the window, most recent first.</summary>
    public IReadOnlyList<SeasonRace> Races { get; }

    public int RaceCount => Races.Count;

    /// <summary>
    /// The most recently run race of <see cref="Year"/>. Held out of training entirely so its
    /// predictions are an honest check rather than a recital of rows the model was fitted on.
    /// Always from <see cref="Year"/> itself — never an earlier season in the window — so that
    /// training with any <see cref="FromYear"/> holds out the same race the holdout page scores.
    /// </summary>
    public SeasonRace? Holdout => Races.FirstOrDefault(r => r.Year == Year);

    /// <summary>Every race except the holdout, most recent first.</summary>
    public IReadOnlyList<SeasonRace> TrainingRaces =>
        Holdout is null ? Races : [.. Races.Where(r => r.SessionKey != Holdout.SessionKey)];

    public IReadOnlyList<DriverRaceFeature> HoldoutFeatures =>
        Holdout is null
            ? []
            : [.. Features.Where(f => f.SessionKey == Holdout.SessionKey).OrderBy(f => f.FinishPosition)];

    /// <param name="context">Database.</param>
    /// <param name="year">Last season to load, and the season the holdout comes from.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <param name="fromYear">First season to load. Defaults to <paramref name="year"/> — a single season.</param>
    public static async Task<SeasonFeatureSet> LoadAsync(
        IApplicationDbContext context,
        int year,
        CancellationToken cancellationToken,
        int? fromYear = null)
    {
        var firstYear = fromYear ?? year;

        // Grands Prix only, matching the feature rebuild — a sprint is a different kind of race
        // and must never become a training row or the holdout. See RaceSession.IsSprint.
        var races = await (
            from session in context.RaceSessions
            join meeting in context.Meetings on session.MeetingKey equals meeting.MeetingKey
            where meeting.Year >= firstYear && meeting.Year <= year && !session.IsSprint && session.IsClassified
            orderby session.DateStart descending
            select new SeasonRace(
                session.SessionKey,
                meeting.MeetingKey,
                meeting.MeetingName,
                meeting.CircuitShortName,
                session.DateStart,
                meeting.Year))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var sessionKeys = races.ConvertAll(r => r.SessionKey);

        var features = await context.DriverRaceFeatures
            .Where(f => sessionKeys.Contains(f.SessionKey))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // A race with no feature rows (missing grid or results) is not part of the usable window.
        var featuredSessionKeys = features.Select(f => f.SessionKey).ToHashSet();
        var usableRaces = races.Where(r => featuredSessionKeys.Contains(r.SessionKey)).ToList();

        return new SeasonFeatureSet(firstYear, year, features, usableRaces);
    }
}

/// <param name="SessionKey">OpenF1 race session key.</param>
/// <param name="MeetingKey">OpenF1 meeting key.</param>
/// <param name="MeetingName">Race weekend name, e.g. "Belgian Grand Prix".</param>
/// <param name="CircuitShortName">Short circuit name, e.g. "Spa-Francorchamps".</param>
/// <param name="DateStart">When the race session started.</param>
/// <param name="Year">Season the race belongs to.</param>
internal sealed record SeasonRace(
    int SessionKey,
    int MeetingKey,
    string MeetingName,
    string CircuitShortName,
    DateTimeOffset DateStart,
    int Year);
