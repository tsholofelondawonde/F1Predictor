namespace F1Predictor.Infrastructure.OpenF1;

/// <summary>
/// Spaces out requests to OpenF1 so a full-season ingest behaves like a considerate client
/// rather than a load test. OpenF1 is free and community-run; being slow is the point.
/// </summary>
/// <remarks>
/// The delay is enforced across all callers via a semaphore, so parallel requests queue rather
/// than each sleeping independently and still arriving together.
/// </remarks>
internal sealed class PolitenessDelayHandler(TimeSpan delay) : DelegatingHandler
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DateTimeOffset _lastRequestAt = DateTimeOffset.MinValue;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var sinceLast = DateTimeOffset.UtcNow - _lastRequestAt;
            if (sinceLast < delay)
            {
                await Task.Delay(delay - sinceLast, cancellationToken);
            }

            _lastRequestAt = DateTimeOffset.UtcNow;
        }
        finally
        {
            _gate.Release();
        }

        return await base.SendAsync(request, cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _gate.Dispose();
        }

        base.Dispose(disposing);
    }
}
