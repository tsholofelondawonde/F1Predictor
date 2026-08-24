namespace F1Predictor.Domain.Championship;

/// <summary>
/// The points on offer for a finishing position.
/// </summary>
/// <remarks>
/// Historical results are never scored with this table — those carry the points OpenF1 awarded,
/// which is authoritative and survives rule changes. This exists only to score races that have
/// not happened yet, where there is no result to read a number off.
/// </remarks>
public static class ChampionshipPoints
{
    private static readonly double[] RaceScale = [25, 18, 15, 12, 10, 8, 6, 4, 2, 1];

    private static readonly double[] SprintScale = [8, 7, 6, 5, 4, 3, 2, 1];

    /// <summary>Points for a win, the most one race can move a championship.</summary>
    public static double MaximumRacePoints => RaceScale[0];

    /// <summary>Points for a sprint win.</summary>
    public static double MaximumSprintPoints => SprintScale[0];

    /// <summary>Points for finishing <paramref name="position"/> in a Grand Prix, 1-based.</summary>
    public static double ForRacePosition(int position) => From(RaceScale, position);

    /// <summary>Points for finishing <paramref name="position"/> in a sprint, 1-based.</summary>
    public static double ForSprintPosition(int position) => From(SprintScale, position);

    /// <summary>Points for a position in a session of either kind.</summary>
    public static double ForPosition(int position, bool isSprint) =>
        isSprint ? ForSprintPosition(position) : ForRacePosition(position);

    /// <summary>
    /// The most a single entrant can still score across the sessions left in the season.
    /// </summary>
    public static double MaximumAvailable(int racesRemaining, int sprintsRemaining) =>
        (racesRemaining * MaximumRacePoints) + (sprintsRemaining * MaximumSprintPoints);

    /// <summary>
    /// The most a constructor can still score, which is nearly double a driver's ceiling: a team
    /// scores with both cars, so a one-two is worth 25 and 18 rather than just the win.
    /// </summary>
    public static double MaximumAvailableForConstructor(int racesRemaining, int sprintsRemaining) =>
        (racesRemaining * (ForRacePosition(1) + ForRacePosition(2))) +
        (sprintsRemaining * (ForSprintPosition(1) + ForSprintPosition(2)));

    /// <summary>
    /// The worst position an entrant could average across the remaining sessions and still
    /// score no more than <paramref name="allowance"/> — or rather, the best such position.
    /// Returns <see langword="null"/> when even scoring nothing would exceed the allowance,
    /// which means the allowance is negative and the contender is already beaten.
    /// </summary>
    public static int? BestAverageWithin(double allowance, int racesRemaining, int sprintsRemaining)
    {
        if (allowance < 0)
        {
            return null;
        }

        // Position 1 is excluded: these scenarios all assume the contender wins every session,
        // so second is the best anyone else can manage.
        const int RunnerUp = 2;
        const int OutOfThePoints = 11;

        for (var position = RunnerUp; position <= OutOfThePoints; position++)
        {
            var total = (ForRacePosition(position) * racesRemaining) +
                        (ForSprintPosition(position) * sprintsRemaining);

            if (total <= allowance)
            {
                return position;
            }
        }

        return OutOfThePoints;
    }

    /// <summary>
    /// The best finishing position worth at least <paramref name="points"/> in a Grand Prix,
    /// or <see langword="null"/> when a win is not enough.
    /// </summary>
    public static int? RacePositionWorth(double points)
    {
        for (var position = 1; position <= RaceScale.Length; position++)
        {
            if (ForRacePosition(position) >= points)
            {
                return position;
            }
        }

        return null;
    }

    private static double From(double[] scale, int position) =>
        position >= 1 && position <= scale.Length ? scale[position - 1] : 0;
}
