namespace F1Predictor.Domain.Championship;

/// <summary>
/// Estimates who wins the two championships by playing the rest of the season out many times.
/// </summary>
/// <remarks>
/// The podium and points-finish classifiers cannot answer this. They need a grid position, which
/// a race that has not qualified does not have, and two marginal probabilities do not describe a
/// finishing order anyway — and a championship is decided by orders, not by margins.
/// <para>
/// So each remaining session is sampled as a whole finishing order from the Plackett–Luce model in
/// <see cref="DriverFormModel"/>, retirements are drawn first and drop those drivers to the back
/// with nothing, points are awarded, and the table is settled by the same count-back the real
/// championship uses. The share of simulated seasons an entrant tops the table is their title
/// probability.
/// </para>
/// <para>
/// The seed is fixed on purpose. The dashboard re-reads this on a timer, and an unseeded run would
/// jitter the headline number by a few tenths on every poll while telling the reader nothing.
/// </para>
/// </remarks>
public static class ChampionshipSimulator
{
    /// <summary>
    /// Enough that a probability is steady to well under a percentage point, and still fast
    /// enough to compute inside a request.
    /// </summary>
    public const int DefaultSimulations = 10_000;

    /// <summary>Matches the trainer's seed. Nothing depends on the value, only on its fixity.</summary>
    public const int DefaultSeed = 42;

    public static ChampionshipForecast Run(
        ChampionshipStandings standings,
        IReadOnlyList<DriverForm> forms,
        IReadOnlyList<RemainingSession> remaining,
        int simulations = DefaultSimulations,
        int seed = DefaultSeed)
    {
        ArgumentNullException.ThrowIfNull(standings);
        ArgumentNullException.ThrowIfNull(forms);
        ArgumentNullException.ThrowIfNull(remaining);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(simulations);

        var field = new SimulationField(standings, forms);
        var random = new DeterministicRandom((ulong)seed);

        var driverPointsPerRun = NewSamples(field.DriverCount, simulations);
        var teamPointsPerRun = NewSamples(field.TeamCount, simulations);
        var driverTitles = new int[field.DriverCount];
        var teamTitles = new int[field.TeamCount];

        for (var run = 0; run < simulations; run++)
        {
            field.ResetToCurrent();

            foreach (var session in remaining)
            {
                SimulateSession(field, session, random);
            }

            field.RollUpTeams();

            driverTitles[field.ChampionDriver()]++;
            teamTitles[field.ChampionTeam()]++;

            Record(driverPointsPerRun, field.DriverPoints, run);
            Record(teamPointsPerRun, field.TeamPoints, run);
        }

        var racesRemaining = remaining.Count(s => !s.IsSprint);

        return new ChampionshipForecast(
            simulations,
            racesRemaining,
            remaining.Count - racesRemaining,
            [.. standings.Drivers.Select((d, i) => new DriverTitleOdds(
                d.DriverNumber, Summarise(driverTitles[i], driverPointsPerRun[i], simulations)))],
            [.. standings.Constructors.Select((c, i) => new ConstructorTitleOdds(
                c.TeamName, Summarise(teamTitles[i], teamPointsPerRun[i], simulations)))]);
    }

    /// <summary>
    /// Runs one session: draw retirements, sample an order from the survivors, award the points.
    /// </summary>
    private static void SimulateSession(SimulationField field, RemainingSession session, DeterministicRandom random)
    {
        var running = 0;

        for (var i = 0; i < field.DriverCount; i++)
        {
            if (random.NextDouble() < field.DnfRates[i])
            {
                continue;
            }

            field.SortKeys[running] = field.LogStrengths[i] + random.NextGumbel();
            field.SortedDrivers[running] = i;
            running++;
        }

        // Ascending, so the fastest sampled key — the winner — ends up last.
        Array.Sort(field.SortKeys, field.SortedDrivers, 0, running);

        for (var rank = 0; rank < running; rank++)
        {
            var driver = field.SortedDrivers[running - 1 - rank];
            var position = rank + 1;

            field.DriverPoints[driver] += ChampionshipPoints.ForPosition(position, session.IsSprint);

            if (position == 1 && !session.IsSprint)
            {
                field.DriverWins[driver]++;
            }
        }
    }

    private static double[][] NewSamples(int entrants, int simulations)
    {
        var samples = new double[entrants][];

        for (var i = 0; i < entrants; i++)
        {
            samples[i] = new double[simulations];
        }

        return samples;
    }

    private static void Record(double[][] samples, double[] totals, int run)
    {
        for (var i = 0; i < totals.Length; i++)
        {
            samples[i][run] = totals[i];
        }
    }

    private static TitleOdds Summarise(int titles, double[] finalPoints, int simulations)
    {
        Array.Sort(finalPoints);

        return new TitleOdds(
            (double)titles / simulations,
            finalPoints.Average(),
            Percentile(finalPoints, 0.10),
            Percentile(finalPoints, 0.90));
    }

    private static double Percentile(double[] sorted, double fraction)
    {
        var rank = (int)Math.Round(fraction * (sorted.Length - 1), MidpointRounding.AwayFromZero);

        return sorted[Math.Clamp(rank, 0, sorted.Length - 1)];
    }
}
