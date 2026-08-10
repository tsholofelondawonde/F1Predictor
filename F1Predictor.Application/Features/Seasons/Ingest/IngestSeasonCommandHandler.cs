using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Abstractions.OpenF1;
using F1Predictor.Domain.RaceData.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace F1Predictor.Application.Features.Seasons.Ingest;

internal sealed class IngestSeasonCommandHandler(
    IOpenF1Client openF1,
    IApplicationDbContext context,
    ILogger<IngestSeasonCommandHandler> logger)
    : ICommandHandler<IngestSeasonCommand, IngestSeasonResponse>
{
    public async Task<Result<IngestSeasonResponse>> Handle(
        IngestSeasonCommand command,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<OpenF1Meeting> meetings;
        try
        {
            meetings = await openF1.GetMeetingsAsync(command.Year, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "OpenF1 was unreachable while listing meetings for {Year}.", command.Year);
            return Result.Failure<IngestSeasonResponse>(Error.Problem(
                "Season.OpenF1Unavailable",
                $"Could not reach OpenF1 to list the {command.Year} season.",
                "The F1 data service could not be reached. Please try again shortly."));
        }

        logger.LogInformation("Found {MeetingCount} meetings for {Year}.", meetings.Count, command.Year);

        var outcomes = new List<IngestedMeeting>(meetings.Count);

        foreach (var meeting in meetings)
        {
            var outcome = await IngestMeetingAsync(meeting, cancellationToken);
            outcomes.Add(outcome);
        }

        var response = new IngestSeasonResponse(
            command.Year,
            meetings.Count,
            outcomes.Count(o => o.Outcome == IngestOutcome.Ingested),
            outcomes);

        return Result.Success(response);
    }

    private async Task<IngestedMeeting> IngestMeetingAsync(OpenF1Meeting meeting, CancellationToken cancellationToken)
    {
        await UpsertMeetingAsync(meeting, cancellationToken);

        var sessions = await openF1.GetSessionsAsync(meeting.MeetingKey, cancellationToken);

        var raceSession = sessions.FirstOrDefault(s =>
            string.Equals(s.SessionType, "Race", StringComparison.Ordinal) &&
            string.Equals(s.SessionName, "Race", StringComparison.Ordinal));

        if (raceSession is null)
        {
            logger.LogInformation("{MeetingName}: no race session yet, skipping.", meeting.MeetingName);
            return new IngestedMeeting(meeting.MeetingName, IngestOutcome.NoRaceSession, 0, 0, 0, 0);
        }

        var alreadyIngested = await context.RaceSessions
            .AnyAsync(s => s.SessionKey == raceSession.SessionKey, cancellationToken);

        if (alreadyIngested)
        {
            logger.LogInformation("{MeetingName}: already ingested, skipping.", meeting.MeetingName);
            return new IngestedMeeting(meeting.MeetingName, IngestOutcome.AlreadyPresent, 0, 0, 0, 0);
        }

        var results = await openF1.GetSessionResultAsync(raceSession.SessionKey, cancellationToken);
        if (results.Count == 0)
        {
            logger.LogInformation("{MeetingName}: race not yet classified, skipping.", meeting.MeetingName);
            return new IngestedMeeting(meeting.MeetingName, IngestOutcome.NotClassified, 0, 0, 0, 0);
        }

        // The grid comes from the qualifying session, not the race session — OpenF1 keys
        // starting_grid by the session that produced the order.
        var qualifyingSession = sessions.FirstOrDefault(s =>
            string.Equals(s.SessionType, "Qualifying", StringComparison.Ordinal) &&
            string.Equals(s.SessionName, "Qualifying", StringComparison.Ordinal));

        IReadOnlyList<OpenF1StartingGrid> grid = qualifyingSession is null
            ? []
            : await openF1.GetStartingGridAsync(qualifyingSession.SessionKey, cancellationToken);

        var pits = await openF1.GetPitStopsAsync(raceSession.SessionKey, cancellationToken);
        var weather = await openF1.GetWeatherAsync(raceSession.SessionKey, cancellationToken);

        await PersistRaceAsync(
            raceSession, qualifyingSession, grid, results, pits, weather, cancellationToken);

        logger.LogInformation(
            "{MeetingName}: ingested ({ResultCount} results, {GridCount} grid rows, {PitCount} pit stops, {WeatherCount} weather readings).",
            meeting.MeetingName, results.Count, grid.Count, pits.Count, weather.Count);

        return new IngestedMeeting(
            meeting.MeetingName,
            IngestOutcome.Ingested,
            results.Count,
            grid.Count,
            pits.Count,
            weather.Count);
    }

    /// <summary>
    /// Writes one race weekend's raw rows in a single save, so a failure part-way through
    /// leaves the session unrecorded and the next run retries it cleanly.
    /// </summary>
    private async Task PersistRaceAsync(
        OpenF1Session raceSession,
        OpenF1Session? qualifyingSession,
        IReadOnlyList<OpenF1StartingGrid> grid,
        IReadOnlyList<OpenF1SessionResult> results,
        IReadOnlyList<OpenF1Pit> pits,
        IReadOnlyList<OpenF1Weather> weather,
        CancellationToken cancellationToken)
    {
        var sessionKey = raceSession.SessionKey;

        context.RaceSessions.Add(new RaceSession
        {
            SessionKey = sessionKey,
            MeetingKey = raceSession.MeetingKey,
            SessionName = raceSession.SessionName,
            SessionType = raceSession.SessionType,
            DateStart = raceSession.DateStart,
            QualifyingSessionKey = qualifyingSession?.SessionKey
        });

        context.StartingGridEntries.AddRange(grid.Select(g => new StartingGridEntry
        {
            SessionKey = sessionKey,
            DriverNumber = g.DriverNumber,
            Position = g.Position,
            LapDuration = g.LapDuration
        }));

        context.SessionResultEntries.AddRange(results.Select(r => new SessionResultEntry
        {
            SessionKey = sessionKey,
            DriverNumber = r.DriverNumber,
            Position = r.Position,
            Dnf = r.Dnf,
            Dns = r.Dns,
            Dsq = r.Dsq
        }));

        context.PitStopEntries.AddRange(pits.Select(p => new PitStopEntry
        {
            SessionKey = sessionKey,
            DriverNumber = p.DriverNumber,
            StopDuration = p.StopDuration,
            LaneDuration = p.LaneDuration
        }));

        context.WeatherReadings.AddRange(weather.Select(w => new WeatherReading
        {
            SessionKey = sessionKey,
            Rainfall = w.Rainfall,
            TrackTemperature = w.TrackTemperature
        }));

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertMeetingAsync(OpenF1Meeting meeting, CancellationToken cancellationToken)
    {
        var existing = await context.Meetings
            .FirstOrDefaultAsync(m => m.MeetingKey == meeting.MeetingKey, cancellationToken);

        if (existing is null)
        {
            context.Meetings.Add(new Meeting
            {
                MeetingKey = meeting.MeetingKey,
                Year = meeting.Year,
                CircuitShortName = meeting.CircuitShortName,
                CountryName = meeting.CountryName,
                MeetingName = meeting.MeetingName,
                DateStart = meeting.DateStart
            });
        }
        else
        {
            existing.Year = meeting.Year;
            existing.CircuitShortName = meeting.CircuitShortName;
            existing.CountryName = meeting.CountryName;
            existing.MeetingName = meeting.MeetingName;
            existing.DateStart = meeting.DateStart;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
