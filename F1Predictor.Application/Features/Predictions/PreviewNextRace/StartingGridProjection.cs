using F1Predictor.Domain.Predictions;
using F1Predictor.Domain.RaceData.Entities;

namespace F1Predictor.Application.Features.Predictions.PreviewNextRace;

/// <summary>
/// Builds the feature rows a race preview is predicted from, either from the published grid or
/// from a projection of it.
/// </summary>
/// <remarks>
/// These rows are never persisted. The predictor takes a whole <see cref="DriverRaceFeature"/>,
/// and there is no result to build one from for a race that has not happened, so they are
/// assembled in memory and handed straight over.
/// </remarks>
internal static class StartingGridProjection
{
    /// <summary>
    /// Uses the real starting order, once qualifying has produced one.
    /// </summary>
    public static Dictionary<int, DriverRaceFeature> FromActualGrid(
        IReadOnlyList<DriverEntry> entries,
        IReadOnlyList<StartingGridEntry> grid,
        IReadOnlyDictionary<int, DriverSeasonForm> history)
    {
        var polePace = DriverRaceFeature.PolePaceOf(grid);

        return entries.ToDictionary(
            entry => entry.DriverNumber,
            entry =>
            {
                var slot = grid.FirstOrDefault(g => g.DriverNumber == entry.DriverNumber);

                // No grid slot means a pit lane start, exactly as in the feature rebuild.
                var position = slot?.Position ?? grid.Count + 1;

                var gap = slot?.LapDuration is { } lap
                    ? lap - polePace
                    : DriverRaceFeature.MissingLapTimeSentinel;

                return Build(entry.DriverNumber, position, (float)gap, history);
            });
    }

    /// <summary>
    /// Projects a grid from recent form, for the days between a race being scheduled and its
    /// qualifying session being run.
    /// </summary>
    /// <remarks>
    /// The projected order is a ranking, not the averages themselves: the model was fitted on grid
    /// slots 1 to 20, and feeding it a field where six drivers all start "4.3rd" would be a
    /// distribution it has never seen.
    /// <para>
    /// Drivers are ranked on their average gap to pole rather than their average grid position, so
    /// that the two features stay rank-consistent — in a real qualifying session the grid *is* the
    /// lap time order, and handing the model a field where third is slower than fourth would be
    /// another shape it was never trained on. Anyone who has never set a time falls back to average
    /// grid position and sorts behind those who have.
    /// </para>
    /// <para>
    /// Drivers with no season form at all go to the back — a rookie stepping into a seat has
    /// nothing to project from, and the response flags them so the number is not read as a real
    /// prediction.
    /// </para>
    /// </remarks>
    public static Dictionary<int, DriverRaceFeature> FromRecentForm(
        IReadOnlyList<DriverEntry> entries,
        IReadOnlyDictionary<int, DriverSeasonForm> history)
    {
        var projected = entries
            .Select(entry => (
                entry.DriverNumber,
                Form: history.GetValueOrDefault(entry.DriverNumber)))
            .OrderBy(pair => pair.Form?.AverageQualiGap is null ? 1 : 0)
            .ThenBy(pair => pair.Form?.AverageQualiGap ?? pair.Form?.AverageGridPosition ?? double.MaxValue)
            .ThenBy(pair => pair.DriverNumber)
            .ToList();

        // Gaps are rebased so the projected pole sitter sits at zero, matching how the real
        // feature is measured.
        var quickest = projected
            .Select(pair => pair.Form?.AverageQualiGap)
            .Where(gap => gap.HasValue)
            .Select(gap => gap!.Value)
            .DefaultIfEmpty(0)
            .Min();

        return projected
            .Select((pair, index) => (pair.DriverNumber, pair.Form, Position: index + 1))
            .ToDictionary(
                pair => pair.DriverNumber,
                pair => Build(
                    pair.DriverNumber,
                    pair.Position,
                    pair.Form?.AverageQualiGap is { } gap
                        ? (float)(gap - quickest)
                        : (float)DriverRaceFeature.MissingLapTimeSentinel,
                    history));
    }

    private static DriverRaceFeature Build(
        int driverNumber,
        int gridPosition,
        float qualiGapToPole,
        IReadOnlyDictionary<int, DriverSeasonForm> history)
    {
        var form = history.GetValueOrDefault(driverNumber);

        return new DriverRaceFeature
        {
            DriverNumber = driverNumber,
            GridPosition = gridPosition,
            QualiGapToPole = qualiGapToPole,

            // Pit strategy is unknowable in advance, so the driver's own season average stands in.
            PitStopCount = form?.AveragePitStops ?? 0,
            AvgPitStopDuration = form?.AveragePitDuration ?? 0,

            // Nothing here forecasts the weather, so every preview assumes a dry race.
            Rainfall = 0
        };
    }
}
