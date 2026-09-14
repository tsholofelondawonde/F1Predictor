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
    /// <summary>
    /// OpenF1's <c>session_type</c> for anything that awards championship points. Both the
    /// Grand Prix and the sprint carry it; they are told apart by <c>session_name</c>.
    /// </summary>
    private const string PointsScoringSessionType = "Race";

    private const string SprintSessionName = "Sprint";
    private const string QualifyingSessionType = "Qualifying";
    private const string SprintQualifyingSessionName = "Sprint Qualifying";
    private const string RaceQualifyingSessionName = "Qualifying";

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

        logger.LogInformation(
            "Found {MeetingCount} meetings for {Year} (force: {Force}).",
            meetings.Count, command.Year, command.Force);

        var outcomes = new List<IngestedMeeting>(meetings.Count);

        foreach (var meeting in meetings)
        {
            var outcome = await IngestMeetingAsync(meeting, command.Force, cancellationToken);
            outcomes.Add(outcome);
        }

        var response = new IngestSeasonResponse(
            command.Year,
            meetings.Count,
            outcomes.Count(o => o.Outcome == IngestOutcome.Ingested),
            outcomes);

        return Result.Success(response);
    }

    private async Task<IngestedMeeting> IngestMeetingAsync(
        OpenF1Meeting meeting,
        bool force,
        CancellationToken cancellationToken)
    {
        await UpsertMeetingAsync(meeting, cancellationToken);

        var sessions = await openF1.GetSessionsAsync(meeting.MeetingKey, cancellationToken);

        // A sprint weekend has two points-scoring sessions, so this is a list rather than a
        // single race. Ordered by date so the sprint is processed before the Grand Prix.
        var pointsScoring = sessions
            .Where(s => string.Equals(s.SessionType, PointsScoringSessionType, StringComparison.Ordinal))
            .OrderBy(s => s.DateStart)
            .ToList();

        if (pointsScoring.Count == 0)
        {
            logger.LogInformation("{MeetingName}: no race session yet, skipping.", meeting.MeetingName);
            return IngestedMeeting.NothingToDo(meeting.MeetingKey, meeting.MeetingName, IngestOutcome.NoRaceSession);
        }

        var totals = SessionTotals.Empty;
        var outcomes = new List<IngestOutcome>(pointsScoring.Count);

        foreach (var session in pointsScoring)
        {
            var (outcome, sessionTotals) = await IngestSessionAsync(
                meeting, session, sessions, force, cancellationToken);

            outcomes.Add(outcome);
            totals += sessionTotals;
        }

        return new IngestedMeeting(
            meeting.MeetingKey,
            meeting.MeetingName,
            Summarise(outcomes),
            outcomes.Count(o => o == IngestOutcome.Ingested),
            outcomes.Count(o => o == IngestOutcome.Scheduled),
            totals.Results,
            totals.Grid,
            totals.PitStops,
            totals.WeatherReadings,
            totals.DriverEntries);
    }

    /// <summary>
    /// Collapses a weekend's per-session outcomes into the one that best describes it, most
    /// significant first: real work done beats a schedule recorded, which beats a no-op.
    /// </summary>
    private static IngestOutcome Summarise(IReadOnlyList<IngestOutcome> outcomes)
    {
        if (outcomes.Contains(IngestOutcome.Ingested)) { return IngestOutcome.Ingested; }
        if (outcomes.Contains(IngestOutcome.Scheduled)) { return IngestOutcome.Scheduled; }
        if (outcomes.Contains(IngestOutcome.AlreadyPresent)) { return IngestOutcome.AlreadyPresent; }

        return IngestOutcome.NoRaceSession;
    }

    private async Task<(IngestOutcome Outcome, SessionTotals Totals)> IngestSessionAsync(
        OpenF1Meeting meeting,
        OpenF1Session session,
        IReadOnlyList<OpenF1Session> allSessions,
        bool force,
        CancellationToken cancellationToken)
    {
        var isSprint = string.Equals(session.SessionName, SprintSessionName, StringComparison.Ordinal);
        var label = $"{meeting.MeetingName} ({session.SessionName})";

        // Cheap early-out before bothering with a lock or the OpenF1 round trips below. The
        // authoritative check — the one that actually decides skip vs. ingest — runs again
        // after the advisory lock is acquired, since a concurrent writer may commit between
        // this read and that point.
        var precheck = await context.RaceSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SessionKey == session.SessionKey, cancellationToken);

        if (precheck is { IsClassified: true } && !force)
        {
            logger.LogInformation("{Label}: already ingested, skipping.", label);
            return (IngestOutcome.AlreadyPresent, SessionTotals.Empty);
        }

        // No reason to hold a DB lock across network I/O, so fetch before taking it.
        var payload = await FetchSessionAsync(session, allSessions, isSprint, cancellationToken);

        var written = await WriteSessionUnderLockAsync(session, isSprint, force, payload, cancellationToken);

        if (!written)
        {
            logger.LogInformation("{Label}: already ingested by a concurrent run, skipping.", label);
            return (IngestOutcome.AlreadyPresent, SessionTotals.Empty);
        }

        var totals = new SessionTotals(
            payload.Results.Count,
            payload.Grid.Count,
            payload.Pits.Count,
            payload.Weather.Count,
            payload.Drivers.Count);

        if (!payload.IsClassified)
        {
            logger.LogInformation(
                "{Label}: scheduled for {DateStart:u}, recorded without results ({GridCount} grid rows, {DriverCount} entries).",
                label, session.DateStart, payload.Grid.Count, payload.Drivers.Count);

            return (IngestOutcome.Scheduled, totals);
        }

        logger.LogInformation(
            "{Label}: ingested ({ResultCount} results, {GridCount} grid rows, {PitCount} pit stops, " +
            "{WeatherCount} weather readings, {DriverCount} entries).",
            label, payload.Results.Count, payload.Grid.Count, payload.Pits.Count,
            payload.Weather.Count, payload.Drivers.Count);

        return (IngestOutcome.Ingested, totals);
    }

    /// <summary>
    /// Clears and re-persists one session's rows inside a transaction guarded by a Postgres
    /// advisory lock keyed on <paramref name="session"/>'s key. Two triggers can otherwise race
    /// to ingest the same session — a manual API call overlapping the Quartz coordinator's
    /// startup tick, or, once this app scales beyond one replica, each replica's own scheduler
    /// firing independently. The lock, held for the life of the transaction, serializes them at
    /// the database so the loser re-checks post-commit state and skips instead of racing the
    /// unique index on <c>DriverEntries</c>.
    /// </summary>
    /// <returns>False if a concurrent run already classified this session and <paramref name="force"/> is not set.</returns>
    private async Task<bool> WriteSessionUnderLockAsync(
        OpenF1Session session,
        bool isSprint,
        bool force,
        SessionPayload payload,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        await context.AcquireSessionAdvisoryLockAsync(session.SessionKey, cancellationToken);

        var existing = await context.RaceSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SessionKey == session.SessionKey, cancellationToken);

        if (existing is { IsClassified: true } && !force)
        {
            return false;
        }

        if (existing is not null)
        {
            await ClearSessionAsync(session.SessionKey, cancellationToken);
        }

        await PersistSessionAsync(session, isSprint, payload, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Pulls everything OpenF1 has for one session. How much that is depends on whether the
    /// session has run — see the remarks on each call below.
    /// </summary>
    private async Task<SessionPayload> FetchSessionAsync(
        OpenF1Session session,
        IReadOnlyList<OpenF1Session> allSessions,
        bool isSprint,
        CancellationToken cancellationToken)
    {
        var results = await openF1.GetSessionResultAsync(session.SessionKey, cancellationToken);
        var isClassified = results.Count > 0;

        // The grid comes from the qualifying session, not the race session — OpenF1 keys
        // starting_grid by the session that produced the order. A sprint takes its order from
        // Sprint Qualifying instead.
        var qualifyingSessionName = isSprint ? SprintQualifyingSessionName : RaceQualifyingSessionName;
        var qualifyingSession = allSessions.FirstOrDefault(s =>
            string.Equals(s.SessionType, QualifyingSessionType, StringComparison.Ordinal) &&
            string.Equals(s.SessionName, qualifyingSessionName, StringComparison.Ordinal));

        // Fetched even when the race has not run: once qualifying is done the real grid exists,
        // and that is what turns a projected preview into a confirmed one.
        IReadOnlyList<OpenF1StartingGrid> grid = qualifyingSession is null
            ? []
            : await openF1.GetStartingGridAsync(qualifyingSession.SessionKey, cancellationToken);

        var drivers = await openF1.GetDriversAsync(session.SessionKey, cancellationToken);

        // Pit stops and weather only exist once cars have run, so an unclassified session skips
        // both calls rather than spending two round trips on a guaranteed empty response.
        IReadOnlyList<OpenF1Pit> pits = isClassified
            ? await openF1.GetPitStopsAsync(session.SessionKey, cancellationToken)
            : [];

        IReadOnlyList<OpenF1Weather> weather = isClassified
            ? await openF1.GetWeatherAsync(session.SessionKey, cancellationToken)
            : [];

        return new SessionPayload(
            isClassified, qualifyingSession?.SessionKey, grid, results, pits, weather, drivers);
    }

    /// <summary>
    /// Removes everything previously stored for a session so it can be written fresh. Used
    /// both by <c>force</c> and by the scheduled-to-classified transition, where the placeholder
    /// row and its entry list have to give way to the real thing.
    /// </summary>
    private async Task ClearSessionAsync(int sessionKey, CancellationToken cancellationToken)
    {
        await context.StartingGridEntries.Where(g => g.SessionKey == sessionKey)
            .ExecuteDeleteAsync(cancellationToken);
        await context.SessionResultEntries.Where(r => r.SessionKey == sessionKey)
            .ExecuteDeleteAsync(cancellationToken);
        await context.PitStopEntries.Where(p => p.SessionKey == sessionKey)
            .ExecuteDeleteAsync(cancellationToken);
        await context.WeatherReadings.Where(w => w.SessionKey == sessionKey)
            .ExecuteDeleteAsync(cancellationToken);
        await context.DriverEntries.Where(d => d.SessionKey == sessionKey)
            .ExecuteDeleteAsync(cancellationToken);
        await context.RaceSessions.Where(s => s.SessionKey == sessionKey)
            .ExecuteDeleteAsync(cancellationToken);

        // ExecuteDelete bypasses the change tracker, so anything it still holds for this
        // session now describes rows that no longer exist.
        context.ChangeTracker.Clear();
    }

    /// <summary>
    /// Writes one session's raw rows in a single save, so a failure part-way through leaves the
    /// session unrecorded and the next run retries it cleanly.
    /// </summary>
    private async Task PersistSessionAsync(
        OpenF1Session session,
        bool isSprint,
        SessionPayload payload,
        CancellationToken cancellationToken)
    {
        context.RaceSessions.Add(new RaceSession
        {
            SessionKey = session.SessionKey,
            MeetingKey = session.MeetingKey,
            SessionName = session.SessionName,
            SessionType = session.SessionType,
            DateStart = session.DateStart,
            QualifyingSessionKey = payload.QualifyingSessionKey,
            IsSprint = isSprint,
            IsClassified = payload.IsClassified
        });

        AddChildRows(session.SessionKey, payload);

        await context.SaveChangesAsync(cancellationToken);
    }

    private void AddChildRows(int sessionKey, SessionPayload payload)
    {
        context.StartingGridEntries.AddRange(payload.Grid.Select(g => new StartingGridEntry
        {
            SessionKey = sessionKey,
            DriverNumber = g.DriverNumber,
            Position = g.Position,
            LapDuration = g.LapDuration
        }));

        context.SessionResultEntries.AddRange(payload.Results.Select(r => new SessionResultEntry
        {
            SessionKey = sessionKey,
            DriverNumber = r.DriverNumber,
            Position = r.Position,
            Points = r.Points ?? 0,
            Dnf = r.Dnf,
            Dns = r.Dns,
            Dsq = r.Dsq
        }));

        context.PitStopEntries.AddRange(payload.Pits.Select(p => new PitStopEntry
        {
            SessionKey = sessionKey,
            DriverNumber = p.DriverNumber,
            StopDuration = p.StopDuration,
            LaneDuration = p.LaneDuration
        }));

        context.WeatherReadings.AddRange(payload.Weather.Select(w => new WeatherReading
        {
            SessionKey = sessionKey,
            Rainfall = w.Rainfall,
            TrackTemperature = w.TrackTemperature
        }));

        // OpenF1 occasionally repeats a driver within one session's entry list; the unique
        // index would reject the batch, so keep the first row per car number.
        context.DriverEntries.AddRange(payload.Drivers
            .GroupBy(d => d.DriverNumber)
            .Select(g => g.First())
            .Select(d => new DriverEntry
            {
                SessionKey = sessionKey,
                DriverNumber = d.DriverNumber,
                FullName = d.FullName,
                NameAcronym = d.NameAcronym,
                TeamName = d.TeamName,
                TeamColour = d.TeamColour
            }));
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

    /// <summary>Everything OpenF1 returned for one session, ready to persist.</summary>
    private sealed record SessionPayload(
        bool IsClassified,
        int? QualifyingSessionKey,
        IReadOnlyList<OpenF1StartingGrid> Grid,
        IReadOnlyList<OpenF1SessionResult> Results,
        IReadOnlyList<OpenF1Pit> Pits,
        IReadOnlyList<OpenF1Weather> Weather,
        IReadOnlyList<OpenF1Driver> Drivers);

    /// <summary>Row counts persisted for one session, summed across a weekend for reporting.</summary>
    private sealed record SessionTotals(
        int Results,
        int Grid,
        int PitStops,
        int WeatherReadings,
        int DriverEntries)
    {
        public static SessionTotals Empty { get; } = new(0, 0, 0, 0, 0);

        public static SessionTotals operator +(SessionTotals a, SessionTotals b) => new(
            a.Results + b.Results,
            a.Grid + b.Grid,
            a.PitStops + b.PitStops,
            a.WeatherReadings + b.WeatherReadings,
            a.DriverEntries + b.DriverEntries);
    }
}
