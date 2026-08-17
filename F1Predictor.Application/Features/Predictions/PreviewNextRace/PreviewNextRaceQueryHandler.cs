using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Abstractions.MachineLearning;
using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Domain.Predictions;
using F1Predictor.Domain.RaceData.Entities;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace F1Predictor.Application.Features.Predictions.PreviewNextRace;

/// <summary>
/// Previews the next Grand Prix, using the real starting grid where there is one and a projection
/// from recent form where there is not.
/// </summary>
/// <remarks>
/// The models take a grid position and a gap to pole, neither of which exists until qualifying has
/// run — which is most of the week. Rather than show nothing until Saturday, an unqualified race is
/// previewed against a grid projected from each driver's recent form, and the response says plainly
/// which of the two it is. The projection is an ordering of season averages, not the averages
/// themselves, so the model still sees a real permutation of grid slots.
/// </remarks>
internal sealed class PreviewNextRaceQueryHandler(
    IApplicationDbContext context,
    IRacePredictor predictor)
    : IQueryHandler<PreviewNextRaceQuery, NextRacePreviewResponse>
{
    public async Task<Result<NextRacePreviewResponse>> Handle(
        PreviewNextRaceQuery query,
        CancellationToken cancellationToken)
    {
        if (!predictor.ModelsAvailable)
        {
            return Result.Failure<NextRacePreviewResponse>(PredictionErrors.ModelsNotTrained);
        }

        var race = await NextRaceAsync(query.Year, cancellationToken);

        if (race is null)
        {
            return Result.Failure<NextRacePreviewResponse>(PredictionErrors.NoUpcomingRace(query.Year));
        }

        var entries = await context.DriverEntries
            .Where(d => d.SessionKey == race.SessionKey)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (entries.Count == 0)
        {
            return Result.Failure<NextRacePreviewResponse>(PredictionErrors.NoEntryList(race.MeetingName));
        }

        var grid = await context.StartingGridEntries
            .Where(g => g.SessionKey == race.SessionKey)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var history = await SeasonFormAsync(query.Year, cancellationToken);

        var features = grid.Count > 0
            ? StartingGridProjection.FromActualGrid(entries, grid, history)
            : StartingGridProjection.FromRecentForm(entries, history);

        var drivers = entries
            .Select(entry => Describe(entry, features[entry.DriverNumber], history))
            .OrderByDescending(d => d.PodiumProbability)
            .ToList();

        return Result.Success(new NextRacePreviewResponse(
            query.Year,
            race.SessionKey,
            race.MeetingKey,
            race.MeetingName,
            race.CircuitShortName,
            race.CountryName,
            race.DateStart,
            race.HasSprint,
            grid.Count > 0,
            drivers));
    }

    private PreviewDriverResponse Describe(
        DriverEntry entry,
        DriverRaceFeature feature,
        Dictionary<int, DriverSeasonForm> history)
    {
        var probabilities = predictor.Predict(feature);

        return new PreviewDriverResponse(
            entry.DriverNumber,
            entry.FullName,
            entry.NameAcronym,
            entry.TeamName,
            entry.TeamColour,
            feature.GridPosition,
            feature.QualiGapToPole,
            history.ContainsKey(entry.DriverNumber),
            probabilities.PodiumProbability,
            probabilities.PointsProbability);
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
    private async Task<UpcomingRace?> NextRaceAsync(int year, CancellationToken cancellationToken)
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
                context.RaceSessions.Any(s => s.MeetingKey == meeting.MeetingKey && s.IsSprint)))
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Each driver's season so far, averaged from the feature rows the models were trained on —
    /// which already exclude sprints and unclassified races.
    /// </summary>
    private async Task<Dictionary<int, DriverSeasonForm>> SeasonFormAsync(
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

    private sealed record UpcomingRace(
        int SessionKey,
        int MeetingKey,
        string MeetingName,
        string CircuitShortName,
        string CountryName,
        DateTimeOffset DateStart,
        bool HasSprint);
}
