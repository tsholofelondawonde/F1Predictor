namespace F1Predictor.Application.Abstractions.OpenF1;

/// <summary>
/// Finds the session that produced a race weekend's starting order. The grid comes from the
/// qualifying session, not the race session — OpenF1 keys <c>starting_grid</c> by the session
/// that produced the order. A sprint takes its order from Sprint Qualifying instead. Shared by
/// ingest and the next-race preview so this rule is defined exactly once.
/// </summary>
internal static class QualifyingSessionMatcher
{
    private const string QualifyingSessionType = "Qualifying";
    private const string SprintQualifyingSessionName = "Sprint Qualifying";
    private const string RaceQualifyingSessionName = "Qualifying";

    public static OpenF1Session? Find(IReadOnlyList<OpenF1Session> sessions, bool isSprint)
    {
        var qualifyingSessionName = isSprint ? SprintQualifyingSessionName : RaceQualifyingSessionName;

        return sessions.FirstOrDefault(s =>
            string.Equals(s.SessionType, QualifyingSessionType, StringComparison.Ordinal) &&
            string.Equals(s.SessionName, qualifyingSessionName, StringComparison.Ordinal));
    }
}
