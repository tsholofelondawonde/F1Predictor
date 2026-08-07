namespace F1Predictor.Application.Abstractions.OpenF1;

/// <summary>
/// Read access to the OpenF1 historical timing API.
/// </summary>
/// <remarks>
/// OpenF1 is a free, community-run service. Implementations are expected to rate-limit
/// themselves; callers should not add their own delays on top.
/// </remarks>
public interface IOpenF1Client
{
    Task<IReadOnlyList<OpenF1Meeting>> GetMeetingsAsync(int year, CancellationToken cancellationToken);

    Task<IReadOnlyList<OpenF1Session>> GetSessionsAsync(int meetingKey, CancellationToken cancellationToken);

    /// <summary>
    /// The grid for a race. Note this is queried with the <em>qualifying</em> session key,
    /// not the race session key.
    /// </summary>
    Task<IReadOnlyList<OpenF1StartingGrid>> GetStartingGridAsync(int sessionKey, CancellationToken cancellationToken);

    Task<IReadOnlyList<OpenF1SessionResult>> GetSessionResultAsync(int sessionKey, CancellationToken cancellationToken);

    Task<IReadOnlyList<OpenF1Pit>> GetPitStopsAsync(int sessionKey, CancellationToken cancellationToken);

    Task<IReadOnlyList<OpenF1Weather>> GetWeatherAsync(int sessionKey, CancellationToken cancellationToken);
}
