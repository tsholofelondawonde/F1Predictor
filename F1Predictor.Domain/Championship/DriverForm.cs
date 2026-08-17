namespace F1Predictor.Domain.Championship;

/// <param name="SessionKey">Session the outcome belongs to.</param>
/// <param name="Finishers">Car numbers that saw the flag, best finish first.</param>
/// <param name="Retired">Car numbers that started but did not finish.</param>
public sealed record RaceOutcome(
    int SessionKey,
    IReadOnlyList<int> Finishers,
    IReadOnlyList<int> Retired);

/// <param name="DriverNumber">Car number.</param>
/// <param name="Strength">
/// Relative pace on a ratio scale, normalised so the field averages 1. A driver with twice
/// another's strength is twice as likely to come out ahead in any head-to-head.
/// </param>
/// <param name="DnfRate">Probability of failing to finish a given race.</param>
/// <param name="Starts">Races the driver has started, which is how far to trust the rest.</param>
public sealed record DriverForm(int DriverNumber, double Strength, double DnfRate, int Starts);

/// <summary>
/// Fits a Plackett–Luce strength and a retirement rate per driver from a season's results.
/// </summary>
/// <remarks>
/// Plackett–Luce treats a finishing order as repeated "best of those left" choices, each driver
/// being picked with probability proportional to their strength. That is what makes a whole
/// finishing order usable as evidence rather than just the winner, which matters a great deal
/// when a season offers only twenty-odd data points.
/// <para>
/// Fitted by the standard MM iteration (Hunter, 2004), which is monotonic and needs no step size.
/// Two adjustments earn their place at this sample size: races are weighted towards the present,
/// because a car developed over a season is not the same car; and retirements are excluded from
/// the ordering rather than counted as a last place, because a blown engine says nothing about
/// pace. Retirement risk is then modelled separately, so the simulation can put a fast driver on
/// the back row of a result without pretending they were slow.
/// </para>
/// </remarks>
public static class DriverFormModel
{
    /// <summary>
    /// Races back over which a result's influence halves. Five is roughly a third of a season —
    /// long enough to keep a fitted strength stable, short enough to follow a mid-season swing.
    /// </summary>
    private const double RecencyHalfLifeRaces = 5.0;

    /// <summary>
    /// Weight of the field's average retirement rate when shrinking a driver's own, in races.
    /// A driver needs several seasons before their raw rate beats the field average as an
    /// estimate, and at fourteen races an unlucky run would otherwise read as unreliability.
    /// </summary>
    private const double DnfShrinkageRaces = 5.0;

    /// <summary>
    /// How many finishing positions carry information. Beyond this a result is read only as
    /// "outside the top ten", not as an exact placing.
    /// </summary>
    /// <remarks>
    /// Plackett–Luce over a full finishing order is punishing about outliers: a driver the model
    /// rates highly finishing fifteenth is so unlikely that one such race drags their fitted pace
    /// down further than six wins push it up. And that tail is exactly where the order says least
    /// — fifteenth versus seventeenth after a first-lap collision is not a statement about pace.
    /// <para>
    /// Worse, ignoring it interacts badly with censoring retirements: a driver who crashes out is
    /// dropped from the race entirely, while one who limps home fifteenth is scored as genuinely
    /// slow. Modelling only the top ten — the positions that actually score — removes the
    /// asymmetry. This is the standard top-K partial ranking likelihood, not an approximation.
    /// </para>
    /// </remarks>
    private const int RankedPositions = 10;

    private const int MaxIterations = 500;
    private const double ConvergenceTolerance = 1e-10;

    /// <summary>Floor for a driver who never once beat anybody, so their strength stays positive.</summary>
    private const double MinimumStrength = 1e-3;

    public static IReadOnlyList<DriverForm> Fit(IReadOnlyList<RaceOutcome> races)
    {
        ArgumentNullException.ThrowIfNull(races);

        var drivers = races
            .SelectMany(r => r.Finishers.Concat(r.Retired))
            .Distinct()
            .Order()
            .ToList();

        if (drivers.Count == 0)
        {
            return [];
        }

        var index = drivers
            .Select((number, i) => (number, i))
            .ToDictionary(pair => pair.number, pair => pair.i);

        var strengths = FitStrengths(races, index);
        var (dnfRates, starts) = EstimateReliability(races, index);

        return [.. drivers.Select((number, i) =>
            new DriverForm(number, strengths[i], dnfRates[i], starts[i]))];
    }

    private static double[] FitStrengths(IReadOnlyList<RaceOutcome> races, Dictionary<int, int> index)
    {
        var strengths = new double[index.Count];
        Array.Fill(strengths, 1.0);

        // Ordered oldest first so the most recent race carries full weight.
        var ordered = races.OrderBy(r => r.SessionKey).ToList();
        var weights = new double[ordered.Count];

        for (var r = 0; r < ordered.Count; r++)
        {
            var racesAgo = ordered.Count - 1 - r;
            weights[r] = Math.Pow(0.5, racesAgo / RecencyHalfLifeRaces);
        }

        for (var iteration = 0; iteration < MaxIterations; iteration++)
        {
            var numerators = new double[strengths.Length];
            var denominators = new double[strengths.Length];

            for (var r = 0; r < ordered.Count; r++)
            {
                Accumulate(ordered[r], weights[r], index, strengths, numerators, denominators);
            }

            if (Update(strengths, numerators, denominators) < ConvergenceTolerance)
            {
                break;
            }
        }

        return strengths;
    }

    /// <summary>
    /// Adds one race's contribution to the MM numerators and denominators.
    /// </summary>
    /// <remarks>
    /// Each finishing order is read as a sequence of choices: at stage <c>s</c> the driver who
    /// finished <c>s</c>-th was chosen from everyone still unplaced. The denominator term for a
    /// driver is the sum of <c>1 / (strength still in the pool)</c> over every stage they were
    /// present for, which is a prefix sum over stages — so this stays linear in the field size
    /// rather than quadratic.
    /// </remarks>
    private static void Accumulate(
        RaceOutcome race,
        double weight,
        Dictionary<int, int> index,
        double[] strengths,
        double[] numerators,
        double[] denominators)
    {
        var order = race.Finishers;
        var n = order.Count;

        if (n < 2)
        {
            return;
        }

        // Total strength still unplaced at the start of each stage.
        var remaining = new double[n + 1];

        for (var s = n - 1; s >= 0; s--)
        {
            remaining[s] = remaining[s + 1] + strengths[index[order[s]]];
        }

        // The final stage is a choice with one candidate and carries no information, and stages
        // past the ranked positions are deliberately not modelled at all.
        var stages = Math.Min(RankedPositions, n - 1);

        var cumulative = new double[n];
        var running = 0.0;

        for (var s = 0; s < stages; s++)
        {
            numerators[index[order[s]]] += weight;
            running += weight / remaining[s];
            cumulative[s] = running;
        }

        // Everyone who finished outside the ranked positions was in the pool for every stage
        // that was modelled, and contributes nothing beyond that.
        for (var s = stages; s < n; s++)
        {
            cumulative[s] = running;
        }

        for (var k = 0; k < n; k++)
        {
            denominators[index[order[k]]] += cumulative[k];
        }
    }

    /// <summary>Applies one MM step and returns how far the largest strength moved.</summary>
    private static double Update(double[] strengths, double[] numerators, double[] denominators)
    {
        var updated = new double[strengths.Length];

        for (var i = 0; i < strengths.Length; i++)
        {
            updated[i] = numerators[i] > 0 && denominators[i] > 0
                ? numerators[i] / denominators[i]
                : MinimumStrength;
        }

        // Only ratios are identified, so pin the scale by making the field average 1.
        var mean = updated.Average();

        if (mean <= 0)
        {
            return 0;
        }

        var largestChange = 0.0;

        for (var i = 0; i < strengths.Length; i++)
        {
            var next = Math.Max(updated[i] / mean, MinimumStrength);
            largestChange = Math.Max(largestChange, Math.Abs(next - strengths[i]));
            strengths[i] = next;
        }

        return largestChange;
    }

    private static (double[] DnfRates, int[] Starts) EstimateReliability(
        IReadOnlyList<RaceOutcome> races,
        Dictionary<int, int> index)
    {
        var starts = new int[index.Count];
        var retirements = new int[index.Count];

        foreach (var race in races)
        {
            foreach (var driver in race.Finishers)
            {
                starts[index[driver]]++;
            }

            foreach (var driver in race.Retired)
            {
                starts[index[driver]]++;
                retirements[index[driver]]++;
            }
        }

        var totalStarts = starts.Sum();
        var fieldRate = totalStarts > 0 ? (double)retirements.Sum() / totalStarts : 0.0;

        var rates = new double[index.Count];

        for (var i = 0; i < rates.Length; i++)
        {
            rates[i] = (retirements[i] + (DnfShrinkageRaces * fieldRate)) /
                       (starts[i] + DnfShrinkageRaces);
        }

        return (rates, starts);
    }
}
