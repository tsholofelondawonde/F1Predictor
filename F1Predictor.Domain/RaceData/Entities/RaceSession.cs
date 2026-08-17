namespace F1Predictor.Domain.RaceData.Entities;

/// <summary>
/// A points-scoring session of a meeting, keyed by OpenF1's natural <c>session_key</c>.
/// </summary>
/// <remarks>
/// This table holds three kinds of row, distinguished by <see cref="IsSprint"/> and
/// <see cref="IsClassified"/>: a completed Grand Prix, a completed sprint, and a race that
/// is scheduled but has not run yet. Only the first kind may be used for training — see
/// the remarks on <see cref="IsSprint"/>.
/// </remarks>
public class RaceSession
{
    public int SessionKey { get; set; }
    public int MeetingKey { get; set; }
    public string SessionName { get; set; } = "";
    public string SessionType { get; set; } = "";
    public DateTimeOffset DateStart { get; set; }

    // The OpenF1 starting_grid endpoint is keyed by the qualifying session, not the race
    // session (verified against live responses), so the race row needs to carry a pointer
    // to its own qualifying session.
    public int? QualifyingSessionKey { get; set; }

    /// <summary>
    /// True for a sprint. OpenF1 gives sprints <c>session_type: "Race"</c> with
    /// <c>session_name: "Sprint"</c>, so they arrive down the same pipe as a Grand Prix.
    /// </summary>
    /// <remarks>
    /// Sprints count towards the championship and must be ingested, but they must never
    /// reach the models: a third of the distance, a different points scale, and typically
    /// no pit stop make "podium" and "points finish" mean something else entirely. Every
    /// query that feeds training or feature engineering filters these out.
    /// </remarks>
    public bool IsSprint { get; set; }

    /// <summary>
    /// True once OpenF1 has published results for the session. Scheduled sessions are
    /// persisted unclassified so the next race is known before it runs.
    /// </summary>
    public bool IsClassified { get; set; }
}
