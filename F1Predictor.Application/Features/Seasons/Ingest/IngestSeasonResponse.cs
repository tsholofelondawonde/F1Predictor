namespace F1Predictor.Application.Features.Seasons.Ingest;

/// <param name="Year">Season that was ingested.</param>
/// <param name="MeetingsFound">Race weekends OpenF1 knows about for that season.</param>
/// <param name="MeetingsIngested">Weekends whose race data was newly persisted by this run.</param>
/// <param name="Meetings">Per-weekend outcome, in the order they were processed.</param>
public sealed record IngestSeasonResponse(
    int Year,
    int MeetingsFound,
    int MeetingsIngested,
    IReadOnlyList<IngestedMeeting> Meetings);

/// <param name="MeetingKey">OpenF1's stable id for the weekend — unique even when two meetings share a name.</param>
/// <param name="MeetingName">Name of the race weekend.</param>
/// <param name="Outcome">What happened to it — see <see cref="IngestOutcome"/>.</param>
/// <param name="SessionsIngested">Points-scoring sessions persisted with results. Two on a sprint weekend.</param>
/// <param name="SessionsScheduled">Sessions recorded without results because they have not run yet.</param>
/// <param name="ResultCount">Classified driver results persisted.</param>
/// <param name="GridCount">Grid entries persisted.</param>
/// <param name="PitStopCount">Pit stops persisted.</param>
/// <param name="WeatherReadingCount">Weather samples persisted.</param>
/// <param name="DriverEntryCount">Entry-list rows persisted — the only source of driver names and teams.</param>
public sealed record IngestedMeeting(
    int MeetingKey,
    string MeetingName,
    IngestOutcome Outcome,
    int SessionsIngested,
    int SessionsScheduled,
    int ResultCount,
    int GridCount,
    int PitStopCount,
    int WeatherReadingCount,
    int DriverEntryCount)
{
    /// <summary>A weekend that produced no writes at all.</summary>
    internal static IngestedMeeting NothingToDo(int meetingKey, string meetingName, IngestOutcome outcome) =>
        new(meetingKey, meetingName, outcome, 0, 0, 0, 0, 0, 0, 0);
}

/// <summary>
/// Why a race weekend was or wasn't ingested. Where a weekend's sessions disagree — a sprint
/// that has run alongside a Grand Prix that hasn't — the most significant outcome wins.
/// </summary>
public enum IngestOutcome
{
    /// <summary>Race data was fetched and persisted by this run.</summary>
    Ingested,
    /// <summary>Already in the database from an earlier run.</summary>
    AlreadyPresent,
    /// <summary>OpenF1 has no race session for the weekend yet.</summary>
    NoRaceSession,
    /// <summary>
    /// No longer produced — an unclassified race is now recorded as <see cref="Scheduled"/>.
    /// Retained so the numeric values clients already map against do not shift.
    /// </summary>
    NotClassified,
    /// <summary>
    /// The race has not run yet. Its session and entry list were recorded anyway so it can be
    /// found as the next race and previewed.
    /// </summary>
    Scheduled
}
