using F1Predictor.Domain.Championship;
using Microsoft.Extensions.Caching.Memory;

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

    public static ChampionshipForecast For(
        IMemoryCache cache,
        SeasonChampionship season,
        int simulations) =>
        cache.GetOrCreate(season.ForecastCacheKey(simulations), entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Lifetime;

            return ChampionshipSimulator.Run(
                season.Standings, season.Forms, season.Remaining, simulations);
        })!;
}
