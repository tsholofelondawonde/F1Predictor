namespace F1Predictor.Application.Features.Championship.GetStandings;

/// <param name="Year">Season the tables describe.</param>
/// <param name="RacesCompleted">Grands Prix run so far.</param>
/// <param name="SprintsCompleted">Sprints run so far.</param>
/// <param name="RacesRemaining">Grands Prix still to come.</param>
/// <param name="SprintsRemaining">Sprints still to come.</param>
/// <param name="PointsStillAvailable">The most any one entrant can still score.</param>
/// <param name="Drivers">Drivers' championship, leader first.</param>
/// <param name="Constructors">Constructors' championship, leader first.</param>
public sealed record StandingsResponse(
    int Year,
    int RacesCompleted,
    int SprintsCompleted,
    int RacesRemaining,
    int SprintsRemaining,
    double PointsStillAvailable,
    IReadOnlyList<DriverStandingResponse> Drivers,
    IReadOnlyList<ConstructorStandingResponse> Constructors);

/// <param name="Position">Championship position, 1-based.</param>
/// <param name="DriverNumber">Car number.</param>
/// <param name="FullName">Driver's full name.</param>
/// <param name="NameAcronym">Three-letter abbreviation.</param>
/// <param name="TeamName">Constructor they currently drive for.</param>
/// <param name="TeamColour">Six-digit hex team colour, no leading hash.</param>
/// <param name="Points">Points scored, sprints included.</param>
/// <param name="Wins">Grand Prix wins.</param>
/// <param name="Podiums">Grand Prix podiums.</param>
public sealed record DriverStandingResponse(
    int Position,
    int DriverNumber,
    string FullName,
    string NameAcronym,
    string TeamName,
    string TeamColour,
    double Points,
    int Wins,
    int Podiums);

/// <param name="Position">Championship position, 1-based.</param>
/// <param name="TeamName">Constructor name.</param>
/// <param name="TeamColour">Six-digit hex team colour, no leading hash.</param>
/// <param name="Points">Combined points of the team's drivers.</param>
/// <param name="Wins">Combined Grand Prix wins.</param>
/// <param name="Podiums">Combined Grand Prix podiums.</param>
/// <param name="DriverNumbers">Car numbers that have scored for the team.</param>
public sealed record ConstructorStandingResponse(
    int Position,
    string TeamName,
    string TeamColour,
    double Points,
    int Wins,
    int Podiums,
    IReadOnlyList<int> DriverNumbers);
