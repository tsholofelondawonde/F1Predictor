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

/// <param name="MeetingName">Name of the race weekend.</param>
/// <param name="Outcome">What happened to it — see <see cref="IngestOutcome"/>.</param>
/// <param name="ResultCount">Classified driver results persisted.</param>
/// <param name="GridCount">Grid entries persisted.</param>
/// <param name="PitStopCount">Pit stops persisted.</param>
/// <param name="WeatherReadingCount">Weather samples persisted.</param>
public sealed record IngestedMeeting(
    string MeetingName,
    IngestOutcome Outcome,
    int ResultCount,
    int GridCount,
    int PitStopCount,
    int WeatherReadingCount);

/// <summary>
/// Why a race weekend was or wasn't ingested.
/// </summary>
public enum IngestOutcome
{
    /// <summary>Race data was fetched and persisted by this run.</summary>
    Ingested,

    /// <summary>Already in the database from an earlier run.</summary>
    AlreadyPresent,

    /// <summary>OpenF1 has no race session for the weekend yet.</summary>
    NoRaceSession,

    /// <summary>The race exists but has not been classified, so there is nothing to learn from.</summary>
    NotClassified
}
