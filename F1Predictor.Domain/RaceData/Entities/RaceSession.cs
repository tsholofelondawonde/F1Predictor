namespace F1Predictor.Domain.RaceData.Entities;

/// <summary>
/// The single race session of a meeting, keyed by OpenF1's natural <c>session_key</c>.
/// </summary>
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
}
