using F1Predictor.Domain.RaceData.Entities;

namespace F1Predictor.Domain.Predictions;

/// <summary>
/// One model-ready row: a single driver's engineered features and labels for a single race.
/// </summary>
/// <remarks>
/// The engineering rules live on this type rather than in the rebuild use case so that the
/// definition of "podium" or "gap to pole" has exactly one home.
/// </remarks>
public class DriverRaceFeature
{
    /// <summary>
    /// Stand-in gap used when a driver set no qualifying time at all — a Q1 crash, a pit lane
    /// start, a grid penalty served from the back. Deliberately far outside the real range so
    /// the model can separate it from a genuine slow lap.
    /// </summary>
    /// <remarks>
    /// This is a sentinel, not an imputation, and is a known rough edge: it puts these drivers
    /// on a scale the normaliser then has to stretch to cover. Proper imputation is a later job.
    /// </remarks>
    public const double MissingLapTimeSentinel = 99;

    public int Id { get; set; }
    public int SessionKey { get; set; }
    public int DriverNumber { get; set; }
    public float GridPosition { get; set; }
    public float QualiGapToPole { get; set; }
    public float PitStopCount { get; set; }
    public float AvgPitStopDuration { get; set; }
    public float Rainfall { get; set; }
    public int FinishPosition { get; set; }
    public bool Podium { get; set; }
    public bool PointsFinish { get; set; }

    /// <summary>
    /// The fastest qualifying lap on the grid, in seconds. Drivers without a time are excluded;
    /// a grid where nobody set a time yields 0, which then makes every gap the raw lap time.
    /// </summary>
    public static double PolePaceOf(IEnumerable<StartingGridEntry> grid) =>
        grid.Where(g => g.LapDuration.HasValue)
            .Select(g => g.LapDuration!.Value)
            .DefaultIfEmpty(0)
            .Min();

    /// <summary>
    /// Whether it rained at any point in the session. OpenF1 samples weather repeatedly, so a
    /// single wet reading is enough to call the race wet.
    /// </summary>
    public static bool RainedIn(IEnumerable<WeatherReading> weather) =>
        weather.Any(w => w.Rainfall > 0);

    /// <summary>
    /// Builds the feature row for one driver. Callers are responsible for filtering out
    /// non-starters and unclassified drivers before calling this.
    /// </summary>
    /// <param name="result">The driver's classified race result. <c>Position</c> must have a value.</param>
    /// <param name="gridEntry">The driver's grid slot, or null if they never appeared on the grid.</param>
    /// <param name="gridCount">Number of grid entries in the session, used for the missing-grid fallback.</param>
    /// <param name="polePace">Pole lap time from <see cref="PolePaceOf"/>.</param>
    /// <param name="driverPits">This driver's pit stops for the session.</param>
    /// <param name="rainedInSession">Result of <see cref="RainedIn"/> for the session.</param>
    public static DriverRaceFeature Create(
        SessionResultEntry result,
        StartingGridEntry? gridEntry,
        int gridCount,
        double polePace,
        IReadOnlyCollection<PitStopEntry> driverPits,
        bool rainedInSession)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(driverPits);

        if (!result.Position.HasValue)
        {
            throw new ArgumentException(
                "Cannot build a feature row for an unclassified driver.", nameof(result));
        }

        // No grid entry means a pit lane start or similar: treat them as starting behind the
        // whole field rather than dropping the row.
        var gridPosition = gridEntry?.Position ?? gridCount + 1;

        var qualiGapToPole = gridEntry?.LapDuration is { } lapDuration
            ? lapDuration - polePace
            : MissingLapTimeSentinel;

        var finishPosition = result.Position.Value;

        return new DriverRaceFeature
        {
            SessionKey = result.SessionKey,
            DriverNumber = result.DriverNumber,
            GridPosition = gridPosition,
            QualiGapToPole = (float)qualiGapToPole,
            PitStopCount = driverPits.Count,
            AvgPitStopDuration = driverPits.Count == 0
                ? 0
                : (float)driverPits.Average(p => p.EffectiveDuration),
            Rainfall = rainedInSession ? 1 : 0,
            FinishPosition = finishPosition,
            Podium = finishPosition <= 3 && !result.Dsq,
            PointsFinish = finishPosition <= 10 && !result.Dsq
        };
    }
}
