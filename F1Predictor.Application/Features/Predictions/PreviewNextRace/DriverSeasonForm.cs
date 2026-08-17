using F1Predictor.Domain.Predictions;

namespace F1Predictor.Application.Features.Predictions.PreviewNextRace;

/// <summary>
/// What one driver's season so far says about how they are likely to start the next race.
/// </summary>
/// <param name="AverageGridPosition">Mean starting position across their races.</param>
/// <param name="AverageQualiGap">
/// Mean gap to pole in seconds. Races where they set no time are left out rather than averaged in
/// at the sentinel, which would otherwise drag a quick driver's gap towards 99 seconds after a
/// single Q1 crash.
/// </param>
/// <param name="AveragePitStops">Mean pit stops per race.</param>
/// <param name="AveragePitDuration">Mean stationary time per stop, in seconds.</param>
internal sealed record DriverSeasonForm(
    double AverageGridPosition,
    double? AverageQualiGap,
    float AveragePitStops,
    float AveragePitDuration)
{
    /// <summary>
    /// Weight of the most recent race relative to one five races earlier. Form matters more than
    /// history when projecting a grid, but a single bad Saturday should not define a driver.
    /// </summary>
    private const double RecencyHalfLifeRaces = 5.0;

    public static DriverSeasonForm From(IEnumerable<DriverRaceFeature> rows)
    {
        // Ordered oldest first so the newest race carries full weight.
        var ordered = rows.OrderBy(f => f.SessionKey).ToList();
        var weights = new double[ordered.Count];

        for (var i = 0; i < ordered.Count; i++)
        {
            weights[i] = Math.Pow(0.5, (ordered.Count - 1 - i) / RecencyHalfLifeRaces);
        }

        var totalWeight = weights.Sum();

        var timedLaps = ordered
            .Select((f, i) => (Feature: f, Weight: weights[i]))
            .Where(pair => pair.Feature.QualiGapToPole < DriverRaceFeature.MissingLapTimeSentinel)
            .ToList();

        return new DriverSeasonForm(
            Weighted(ordered.Select((f, i) => ((double)f.GridPosition, weights[i])), totalWeight),
            timedLaps.Count > 0
                ? Weighted(
                    timedLaps.Select(pair => ((double)pair.Feature.QualiGapToPole, pair.Weight)),
                    timedLaps.Sum(pair => pair.Weight))
                : null,
            (float)Weighted(ordered.Select((f, i) => ((double)f.PitStopCount, weights[i])), totalWeight),
            (float)Weighted(ordered.Select((f, i) => ((double)f.AvgPitStopDuration, weights[i])), totalWeight));
    }

    private static double Weighted(IEnumerable<(double Value, double Weight)> samples, double totalWeight) =>
        totalWeight > 0 ? samples.Sum(s => s.Value * s.Weight) / totalWeight : 0;
}
