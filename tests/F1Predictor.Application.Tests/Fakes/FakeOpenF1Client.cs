using F1Predictor.Application.Abstractions.OpenF1;

namespace F1Predictor.Application.Tests.Fakes;

/// <summary>
/// Only the members exercised by <c>PreviewNextRaceQueryHandler</c>'s qualifying-ready check are
/// implemented; the rest throw so a test that accidentally reaches them fails loudly.
/// </summary>
internal sealed class FakeOpenF1Client : IOpenF1Client
{
    public IReadOnlyList<OpenF1Session> Sessions { get; set; } = [];
    public IReadOnlyList<OpenF1StartingGrid> Grid { get; set; } = [];
    public Exception? Throws { get; set; }

    public int GetSessionsCallCount { get; private set; }
    public int GetStartingGridCallCount { get; private set; }

    public Task<IReadOnlyList<OpenF1Session>> GetSessionsAsync(int meetingKey, CancellationToken cancellationToken)
    {
        GetSessionsCallCount++;
        if (Throws is not null)
        {
            throw Throws;
        }

        return Task.FromResult(Sessions);
    }

    public Task<IReadOnlyList<OpenF1StartingGrid>> GetStartingGridAsync(int sessionKey, CancellationToken cancellationToken)
    {
        GetStartingGridCallCount++;
        if (Throws is not null)
        {
            throw Throws;
        }

        return Task.FromResult(Grid);
    }

    public Task<IReadOnlyList<OpenF1Meeting>> GetMeetingsAsync(int year, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Not exercised by PreviewNextRaceQueryHandler.");

    public Task<IReadOnlyList<OpenF1SessionResult>> GetSessionResultAsync(int sessionKey, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Not exercised by PreviewNextRaceQueryHandler.");

    public Task<IReadOnlyList<OpenF1Pit>> GetPitStopsAsync(int sessionKey, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Not exercised by PreviewNextRaceQueryHandler.");

    public Task<IReadOnlyList<OpenF1Weather>> GetWeatherAsync(int sessionKey, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Not exercised by PreviewNextRaceQueryHandler.");

    public Task<IReadOnlyList<OpenF1Driver>> GetDriversAsync(int sessionKey, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Not exercised by PreviewNextRaceQueryHandler.");
}
