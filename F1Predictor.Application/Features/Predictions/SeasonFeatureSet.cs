using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Domain.Predictions;
using Microsoft.EntityFrameworkCore;

namespace F1Predictor.Application.Features.Predictions;

/// <summary>
/// A season's feature rows plus the race metadata needed to split them, loaded once.
/// Shared by training and holdout reporting so both agree on which race is held out.
/// </summary>
internal sealed class SeasonFeatureSet
{
    private SeasonFeatureSet(int year, IReadOnlyList<DriverRaceFeature> features, IReadOnlyList<SeasonRace> races)
    {
        Year = year;
        Features = features;
        Races = races;
    }

    public int Year { get; }

    public IReadOnlyList<DriverRaceFeature> Features { get; }

    /// <summary>Races that produced feature rows, most recent first.</summary>
    public IReadOnlyList<SeasonRace> Races { get; }

    public int RaceCount => Races.Count;

    /// <summary>
    /// The most recently run race of the season. Held out of training entirely so its
    /// predictions are an honest check rather than a recital of rows the model was fitted on.
    /// </summary>
    public SeasonRace? Holdout => Races.Count > 0 ? Races[0] : null;

    public IReadOnlyList<DriverRaceFeature> TrainingFeatures =>
        Holdout is null
            ? Features
            : [.. Features.Where(f => f.SessionKey != Holdout.SessionKey)];

    public IReadOnlyList<DriverRaceFeature> HoldoutFeatures =>
        Holdout is null
            ? []
            : [.. Features.Where(f => f.SessionKey == Holdout.SessionKey).OrderBy(f => f.FinishPosition)];

    public static async Task<SeasonFeatureSet> LoadAsync(
        IApplicationDbContext context,
        int year,
        CancellationToken cancellationToken)
    {
        // Grands Prix only, matching the feature rebuild — a sprint is a different kind of race
        // and must never become a training row or the holdout. See RaceSession.IsSprint.
        var races = await (
            from session in context.RaceSessions
            join meeting in context.Meetings on session.MeetingKey equals meeting.MeetingKey
            where meeting.Year == year && !session.IsSprint && session.IsClassified
            orderby session.DateStart descending
            select new SeasonRace(
                session.SessionKey,
                meeting.MeetingKey,
                meeting.MeetingName,
                meeting.CircuitShortName,
                session.DateStart))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var sessionKeys = races.ConvertAll(r => r.SessionKey);

        var features = await context.DriverRaceFeatures
            .Where(f => sessionKeys.Contains(f.SessionKey))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // A race with no feature rows (missing grid or results) is not part of the usable season.
        var featuredSessionKeys = features.Select(f => f.SessionKey).ToHashSet();
        var usableRaces = races.Where(r => featuredSessionKeys.Contains(r.SessionKey)).ToList();

        return new SeasonFeatureSet(year, features, usableRaces);
    }
}

/// <param name="SessionKey">OpenF1 race session key.</param>
/// <param name="MeetingKey">OpenF1 meeting key.</param>
/// <param name="MeetingName">Race weekend name, e.g. "Belgian Grand Prix".</param>
/// <param name="CircuitShortName">Short circuit name, e.g. "Spa-Francorchamps".</param>
/// <param name="DateStart">When the race session started.</param>
internal sealed record SeasonRace(
    int SessionKey,
    int MeetingKey,
    string MeetingName,
    string CircuitShortName,
    DateTimeOffset DateStart);
