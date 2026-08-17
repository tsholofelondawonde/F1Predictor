using F1Predictor.Domain.RaceData.Entities;

namespace F1Predictor.Domain.Championship;

/// <summary>
/// Both championship tables for a season, built from the results actually recorded.
/// </summary>
/// <remarks>
/// Points are summed straight from <see cref="SessionResultEntry.Points"/> rather than derived
/// from finishing position, so sprint scoring and any rule change come from OpenF1 rather than
/// from a table here. Sprints therefore count towards points but not towards the win and podium
/// tallies, which follow the sporting regulations in referring to Grands Prix only.
/// </remarks>
public sealed class ChampionshipStandings
{
    private ChampionshipStandings(
        IReadOnlyList<DriverStanding> drivers,
        IReadOnlyList<ConstructorStanding> constructors)
    {
        Drivers = drivers;
        Constructors = constructors;
    }

    /// <summary>Drivers' championship, leader first.</summary>
    public IReadOnlyList<DriverStanding> Drivers { get; }

    /// <summary>Constructors' championship, leader first.</summary>
    public IReadOnlyList<ConstructorStanding> Constructors { get; }

    public static ChampionshipStandings Build(
        IReadOnlyList<SessionResultEntry> results,
        IReadOnlySet<int> sprintSessionKeys,
        IReadOnlyDictionary<int, DriverEntry> driversByNumber)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(sprintSessionKeys);
        ArgumentNullException.ThrowIfNull(driversByNumber);

        var tallies = new Dictionary<int, Tally>();

        foreach (var result in results)
        {
            if (!tallies.TryGetValue(result.DriverNumber, out var tally))
            {
                tally = new Tally();
                tallies[result.DriverNumber] = tally;
            }

            tally.Add(result, sprintSessionKeys.Contains(result.SessionKey));
        }

        var drivers = BuildDrivers(tallies, driversByNumber);
        var constructors = BuildConstructors(drivers, tallies);

        return new ChampionshipStandings(drivers, constructors);
    }

    private static List<DriverStanding> BuildDrivers(
        Dictionary<int, Tally> tallies,
        IReadOnlyDictionary<int, DriverEntry> driversByNumber) =>
        [.. tallies
            .Select(pair =>
            {
                driversByNumber.TryGetValue(pair.Key, out var entry);
                var tally = pair.Value;

                return new DriverStanding(
                    0,
                    pair.Key,
                    entry?.FullName ?? $"Car {pair.Key}",
                    entry?.NameAcronym ?? pair.Key.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    entry?.TeamName ?? "Unknown",
                    entry?.TeamColour ?? "",
                    tally.Points,
                    tally.Wins,
                    tally.Podiums,
                    tally.PositionCounts);
            })
            .Order(StandingComparer<DriverStanding>.Instance)
            .Select((standing, index) => standing with { Position = index + 1 })];

    private static List<ConstructorStanding> BuildConstructors(
        IEnumerable<DriverStanding> drivers,
        Dictionary<int, Tally> tallies) =>
        [.. drivers
            .GroupBy(d => d.TeamName, StringComparer.Ordinal)
            .Select(team => new ConstructorStanding(
                0,
                team.Key,
                team.First().TeamColour,
                team.Sum(d => d.Points),
                team.Sum(d => d.Wins),
                team.Sum(d => d.Podiums),
                [.. team.Select(d => d.DriverNumber).Order()],
                Tally.CombineCounts(team.Select(d => tallies[d.DriverNumber]))))
            .Order(StandingComparer<ConstructorStanding>.Instance)
            .Select((standing, index) => standing with { Position = index + 1 })];

    /// <summary>Running totals for one driver while the results are walked.</summary>
    private sealed class Tally
    {
        /// <summary>Finishes counted per position, index 0 being a win. Used for the count-back.</summary>
        private readonly int[] _positionCounts = new int[PositionCountDepth];

        internal const int PositionCountDepth = 10;

        public double Points { get; private set; }

        public int Wins { get; private set; }

        public int Podiums { get; private set; }

        public int[] PositionCounts => _positionCounts;

        public void Add(SessionResultEntry result, bool isSprint)
        {
            Points += result.Points;

            if (isSprint || result.Dsq || result.Position is not { } position)
            {
                return;
            }

            if (position <= PositionCountDepth)
            {
                _positionCounts[position - 1]++;
            }

            if (position == 1) { Wins++; }
            if (position <= 3) { Podiums++; }
        }

        public static int[] CombineCounts(IEnumerable<Tally> tallies)
        {
            var combined = new int[PositionCountDepth];

            foreach (var tally in tallies)
            {
                for (var i = 0; i < PositionCountDepth; i++)
                {
                    combined[i] += tally.PositionCounts[i];
                }
            }

            return combined;
        }
    }
}
