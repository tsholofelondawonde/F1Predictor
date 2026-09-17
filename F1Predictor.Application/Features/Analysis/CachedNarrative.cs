using Microsoft.Extensions.Caching.Hybrid;

namespace F1Predictor.Application.Features.Analysis;

/// <summary>
/// Caches one generated narrative string under a caller-supplied key, for the ten minutes a
/// narrative stays true to the same underlying prediction — the same shape as
/// <c>CachedForecast</c>, one level below a full response record.
/// </summary>
internal static class CachedNarrative
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    public static ValueTask<string> For(HybridCache cache, string key, Func<CancellationToken, ValueTask<string>> generate, CancellationToken cancellationToken) =>
        cache.GetOrCreateAsync(key, generate, new HybridCacheEntryOptions { Expiration = Lifetime, LocalCacheExpiration = Lifetime }, cancellationToken: cancellationToken);
}
