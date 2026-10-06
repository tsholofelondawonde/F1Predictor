using System.Globalization;
using Microsoft.Extensions.Caching.Hybrid;

namespace F1Predictor.Application.Features.Predictions.PreviewNextRace;

/// <summary>
/// Caches whether OpenF1 already has a qualifying grid for a meeting, for the five minutes the
/// next-race page's 60-second poll would otherwise spend re-asking OpenF1 the same question —
/// same shape as <c>CachedForecast</c>/<c>CachedNarrative</c>.
/// </summary>
internal static class CachedQualifyingCheck
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    public static ValueTask<bool> For(
        HybridCache cache,
        int meetingKey,
        Func<CancellationToken, ValueTask<bool>> check,
        CancellationToken cancellationToken) =>
        cache.GetOrCreateAsync(
            string.Create(CultureInfo.InvariantCulture, $"qualifying-ready:{meetingKey}"),
            check,
            new HybridCacheEntryOptions { Expiration = Lifetime, LocalCacheExpiration = Lifetime },
            cancellationToken: cancellationToken);
}
