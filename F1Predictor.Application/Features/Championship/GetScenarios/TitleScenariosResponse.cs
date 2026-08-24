namespace F1Predictor.Application.Features.Championship.GetScenarios;

/// <param name="Year">Season being analysed.</param>
/// <param name="RacesRemaining">Grands Prix still to run.</param>
/// <param name="SprintsRemaining">Sprints still to run.</param>
/// <param name="PointsStillAvailable">The most any one driver can still score.</param>
/// <param name="LeaderName">Whoever currently leads the championship.</param>
/// <param name="Contenders">One entry per driver covered, championship order.</param>
public sealed record TitleScenariosResponse(
    int Year,
    int RacesRemaining,
    int SprintsRemaining,
    double PointsStillAvailable,
    string LeaderName,
    IReadOnlyList<TitleScenarioResponse> Contenders);

/// <param name="Position">Current championship position.</param>
/// <param name="DriverNumber">Car number.</param>
/// <param name="FullName">Driver's full name.</param>
/// <param name="NameAcronym">Three-letter abbreviation.</param>
/// <param name="TeamName">Constructor.</param>
/// <param name="TeamColour">Six-digit hex team colour, no leading hash.</param>
/// <param name="Points">Points scored so far.</param>
/// <param name="PointsBehindLeader">Gap to the leader; zero for the leader.</param>
/// <param name="MaximumPossiblePoints">Their total if they won everything left.</param>
/// <param name="IsLeader">Whether they currently lead.</param>
/// <param name="IsMathematicallyAlive">Whether any sequence of results still gives them the title.</param>
/// <param name="HasClinchedTitle">Whether the title is already theirs whatever happens.</param>
/// <param name="PointsToClinch">Points the leader still needs to be uncatchable; null for anyone else.</param>
/// <param name="LeaderMustAverageNoBetterThan">
/// Assuming this driver wins every remaining session, the best average finishing position the
/// leader could still manage and lose. Null for the leader and for anyone already eliminated.
/// </param>
/// <param name="RequiredPointsPerRace">
/// Points per remaining race needed to overhaul the leader if the leader keeps to their season
/// average. Above 25 means no run of results is enough on its own — the leader has to drop points.
/// </param>
/// <param name="RequiredAveragePosition">The finishing position worth that many points, if one exists.</param>
/// <param name="TitleProbability">Share of simulated seasons this driver took the title, 0 to 1.</param>
/// <param name="Requirement">Plain-English summary of what has to happen.</param>
public sealed record TitleScenarioResponse(
    int Position,
    int DriverNumber,
    string FullName,
    string NameAcronym,
    string TeamName,
    string TeamColour,
    double Points,
    double PointsBehindLeader,
    double MaximumPossiblePoints,
    bool IsLeader,
    bool IsMathematicallyAlive,
    bool HasClinchedTitle,
    double? PointsToClinch,
    int? LeaderMustAverageNoBetterThan,
    double? RequiredPointsPerRace,
    int? RequiredAveragePosition,
    double TitleProbability,
    string Requirement);
