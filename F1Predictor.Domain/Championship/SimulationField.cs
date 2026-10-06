namespace F1Predictor.Domain.Championship;

/// <summary>
/// The mutable state one simulation run works on, laid out as parallel arrays indexed by the
/// entrant's place in the standings it was built from.
/// </summary>
/// <remarks>
/// Allocated once and reset per run rather than rebuilt: ten thousand runs over a dozen sessions
/// would otherwise spend most of their time in the garbage collector rather than the model.
/// </remarks>
internal sealed class SimulationField
{
    private readonly double[] _startingDriverPoints;
    private readonly int[] _startingDriverWins;
    private readonly int[] _driverTeam;
    private readonly double[] _paceStandardError;
    private readonly double[] _fittedLogStrengths;

    public SimulationField(ChampionshipStandings standings, IReadOnlyList<DriverForm> forms)
    {
        var formsByDriver = forms.ToDictionary(f => f.DriverNumber);
        var averageDnfRate = forms.Count > 0 ? forms.Average(f => f.DnfRate) : 0.0;

        var teamIndex = standings.Constructors
            .Select((team, index) => (team.TeamName, index))
            .ToDictionary(pair => pair.TeamName, pair => pair.index, StringComparer.Ordinal);

        DriverCount = standings.Drivers.Count;
        TeamCount = standings.Constructors.Count;

        _startingDriverPoints = new double[DriverCount];
        _startingDriverWins = new int[DriverCount];
        _driverTeam = new int[DriverCount];
        _paceStandardError = new double[DriverCount];

        LogStrengths = new double[DriverCount];
        DnfRates = new double[DriverCount];

        for (var i = 0; i < DriverCount; i++)
        {
            var standing = standings.Drivers[i];

            _startingDriverPoints[i] = standing.Points;
            _startingDriverWins[i] = standing.Wins;
            _driverTeam[i] = teamIndex[standing.TeamName];

            // A driver with no fitted form has never finished a race this season. Treating them
            // as an average car is generous, but they are not in title contention either way.
            var form = formsByDriver.GetValueOrDefault(standing.DriverNumber);

            LogStrengths[i] = Math.Log(form?.Strength ?? 1.0);
            DnfRates[i] = form?.DnfRate ?? averageDnfRate;

            // A pace estimate from n races is uncertain by roughly 1/sqrt(n); a driver with no
            // record at all is uncertain by the full unit.
            _paceStandardError[i] = 1.0 / Math.Sqrt(Math.Max(form?.Starts ?? 0, 1));
        }

        _fittedLogStrengths = (double[])LogStrengths.Clone();

        DriverPoints = new double[DriverCount];
        DriverWins = new int[DriverCount];
        TeamPoints = new double[TeamCount];
        TeamWins = new int[TeamCount];
        SortKeys = new double[DriverCount];
        SortedDrivers = new int[DriverCount];
    }

    public int DriverCount { get; }

    public int TeamCount { get; }

    /// <summary>
    /// The pace each driver races at in the current run: the fitted value, nudged by
    /// <see cref="DrawSeasonForm"/>.
    /// </summary>
    public double[] LogStrengths { get; }

    public double[] DnfRates { get; }

    public double[] DriverPoints { get; }

    public int[] DriverWins { get; }

    public double[] TeamPoints { get; }

    public int[] TeamWins { get; }

    /// <summary>Scratch buffer for one session's sampled Plackett–Luce keys.</summary>
    public double[] SortKeys { get; }

    /// <summary>Scratch buffer holding driver indices alongside <see cref="SortKeys"/>.</summary>
    public int[] SortedDrivers { get; }

    /// <summary>
    /// Redraws every driver's pace for one simulated season, around the fitted value.
    /// </summary>
    /// <remarks>
    /// The fit is an estimate from a handful of races, not the truth, and treating it as exact
    /// makes every remaining race an independent coin toss weighted the same way — which averages
    /// out over a season and turns a modest edge into near-certainty. One draw per driver,
    /// held for the whole remaining calendar, keeps the uncertainty about *who is faster* from
    /// cancelling race by race. <paramref name="scale"/> of 0 switches it off.
    /// </remarks>
    public void DrawSeasonForm(DeterministicRandom random, double scale)
    {
        for (var i = 0; i < DriverCount; i++)
        {
            LogStrengths[i] = _fittedLogStrengths[i] +
                              (scale > 0 ? scale * _paceStandardError[i] * random.NextGaussian() : 0.0);
        }
    }

    public void ResetToCurrent()
    {
        Array.Copy(_startingDriverPoints, DriverPoints, DriverCount);
        Array.Copy(_startingDriverWins, DriverWins, DriverCount);
    }

    /// <summary>
    /// Sums the simulated drivers' totals into their constructors. Starting points come along
    /// for the ride, because a constructor's total is exactly its drivers' totals.
    /// </summary>
    public void RollUpTeams()
    {
        Array.Clear(TeamPoints);
        Array.Clear(TeamWins);

        for (var i = 0; i < DriverCount; i++)
        {
            TeamPoints[_driverTeam[i]] += DriverPoints[i];
            TeamWins[_driverTeam[i]] += DriverWins[i];
        }
    }

    public int ChampionDriver() => Champion(DriverCount, DriverPoints, DriverWins);

    public int ChampionTeam() => Champion(TeamCount, TeamPoints, TeamWins);

    /// <summary>
    /// Settles a simulated table: most points, then most wins, and failing both, whoever was
    /// already ahead. That last step stands in for the deeper count-back on second places and
    /// below, which decides a title roughly never and would cost a sort every run.
    /// </summary>
    private static int Champion(int count, double[] points, int[] wins)
    {
        // Points come in halves at worst, so anything closer than this is the same total.
        const double Tolerance = 1e-9;

        var leader = 0;

        for (var i = 1; i < count; i++)
        {
            var margin = points[i] - points[leader];

            if (margin > Tolerance || (Math.Abs(margin) < Tolerance && wins[i] > wins[leader]))
            {
                leader = i;
            }
        }

        return leader;
    }
}
