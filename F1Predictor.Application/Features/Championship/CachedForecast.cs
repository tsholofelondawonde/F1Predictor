using F1Predictor.Domain.Championship;
using Microsoft.Extensions.Caching.Hybrid;

namespace F1Predictor.Application.Features.Championship;

/// <summary>
/// Runs the championship simulation, or hands back the last run if the results have not moved.
/// </summary>
/// <remarks>
/// The dashboard re-reads the forecast on a timer and the scenarios use case needs the same
/// numbers, so an uncached simulation would be re-run several times a minute for an answer that
/// only changes when a race is ingested. The cache key carries the latest classified session, so
/// a new result invalidates it on its own; the expiry is only a backstop.
/// </remarks>
internal static class CachedForecast
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    public static ValueTask<ChampionshipForecast> For(
        HybridCache cache,
        SeasonChampionship season,
        int simulations,
        CancellationToken cancellationToken) =>
        cache.GetOrCreateAsync(
            season.ForecastCacheKey(simulations),
            (season, simulations),
            static (state, _) => ValueTask.FromResult(ChampionshipSimulator.Run(
                state.season.Standings, state.season.Forms, state.season.Remaining, state.simulations)),
            new HybridCacheEntryOptions { Expiration = Lifetime, LocalCacheExpiration = Lifetime },
            cancellationToken: cancellationToken);
}
