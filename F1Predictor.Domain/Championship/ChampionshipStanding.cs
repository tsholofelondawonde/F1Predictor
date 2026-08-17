namespace F1Predictor.Domain.Championship;

/// <summary>
/// What the count-back needs to separate two entrants level on points.
/// </summary>
public interface IChampionshipEntrant
{
    double Points { get; }

    /// <summary>Grand Prix finishes counted by position, index 0 being a win.</summary>
    IReadOnlyList<int> PositionCounts { get; }
}

/// <param name="Position">Place in the championship, 1-based.</param>
/// <param name="DriverNumber">Car number.</param>
/// <param name="FullName">Driver's full name.</param>
/// <param name="NameAcronym">Three-letter abbreviation.</param>
/// <param name="TeamName">Constructor they currently drive for.</param>
/// <param name="TeamColour">Six-digit hex team colour, no leading hash.</param>
/// <param name="Points">Championship points, sprints included.</param>
/// <param name="Wins">Grand Prix wins.</param>
/// <param name="Podiums">Grand Prix podiums.</param>
/// <param name="PositionCounts">Grand Prix finishes by position, for the count-back.</param>
public sealed record DriverStanding(
    int Position,
    int DriverNumber,
    string FullName,
    string NameAcronym,
    string TeamName,
    string TeamColour,
    double Points,
    int Wins,
    int Podiums,
    IReadOnlyList<int> PositionCounts) : IChampionshipEntrant;

/// <param name="Position">Place in the championship, 1-based.</param>
/// <param name="TeamName">Constructor name.</param>
/// <param name="TeamColour">Six-digit hex team colour, no leading hash.</param>
/// <param name="Points">Combined points of every driver who scored for the team.</param>
/// <param name="Wins">Combined Grand Prix wins.</param>
/// <param name="Podiums">Combined Grand Prix podiums.</param>
/// <param name="DriverNumbers">Car numbers that have scored for the team this season.</param>
/// <param name="PositionCounts">Combined finishes by position, for the count-back.</param>
public sealed record ConstructorStanding(
    int Position,
    string TeamName,
    string TeamColour,
    double Points,
    int Wins,
    int Podiums,
    IReadOnlyList<int> DriverNumbers,
    IReadOnlyList<int> PositionCounts) : IChampionshipEntrant;

/// <summary>
/// Orders a championship table: most points first, and where those are level, the entrant with
/// more wins, then more second places, then more thirds, and so on — the count-back the sporting
/// regulations prescribe.
/// </summary>
public sealed class StandingComparer<T> : IComparer<T>
    where T : IChampionshipEntrant
{
    public static StandingComparer<T> Instance { get; } = new();

    public int Compare(T? x, T? y)
    {
        if (ReferenceEquals(x, y)) { return 0; }
        if (x is null) { return 1; }
        if (y is null) { return -1; }

        var byPoints = y.Points.CompareTo(x.Points);

        if (byPoints != 0)
        {
            return byPoints;
        }

        var depth = Math.Min(x.PositionCounts.Count, y.PositionCounts.Count);

        for (var position = 0; position < depth; position++)
        {
            var byCount = y.PositionCounts[position].CompareTo(x.PositionCounts[position]);

            if (byCount != 0)
            {
                return byCount;
            }
        }

        return 0;
    }
}
