using System.Globalization;

namespace F1Predictor.Domain.Championship;

/// <param name="DriverNumber">Car number.</param>
/// <param name="FullName">Driver's full name.</param>
/// <param name="NameAcronym">Three-letter abbreviation.</param>
/// <param name="TeamName">Constructor.</param>
/// <param name="TeamColour">Six-digit hex team colour, no leading hash.</param>
/// <param name="Position">Current championship position.</param>
/// <param name="Points">Points scored so far.</param>
/// <param name="PointsBehindLeader">Gap to the championship leader; zero for the leader.</param>
/// <param name="PointsStillAvailable">The most anyone can still score this season.</param>
/// <param name="MaximumPossiblePoints">This driver's total if they won everything left.</param>
/// <param name="IsLeader">Whether they currently lead the championship.</param>
/// <param name="IsMathematicallyAlive">Whether any sequence of results still gives them the title.</param>
/// <param name="HasClinchedTitle">Whether the title is already theirs whatever happens.</param>
/// <param name="PointsToClinch">Points the leader still needs to be uncatchable; null for anyone else.</param>
/// <param name="LeaderMustAverageNoBetterThan">
/// If this driver wins every remaining session, the best average finishing position the leader
/// could still manage and lose. Null for the leader, and for anyone already eliminated.
/// </param>
/// <param name="RequiredPointsPerRace">
/// Points per remaining race needed to overhaul the leader, if the leader keeps scoring at their
/// season average. Null for the leader.
/// </param>
/// <param name="RequiredAveragePosition">The finishing position worth that many points, if one exists.</param>
/// <param name="TitleProbability">Share of simulated seasons this driver took the title.</param>
/// <param name="Requirement">Plain-English summary of what has to happen.</param>
public sealed record TitleScenario(
    int DriverNumber,
    string FullName,
    string NameAcronym,
    string TeamName,
    string TeamColour,
    int Position,
    double Points,
    double PointsBehindLeader,
    double PointsStillAvailable,
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

/// <summary>
/// Works out what each contender actually needs, as arithmetic rather than simulation.
/// </summary>
/// <remarks>
/// The simulation says how likely a title is; this says what it would take. Both are worth having:
/// a two percent chance and "must win out while the leader scores nothing" are the same fact told
/// two ways, and the second is the one a reader can picture.
/// </remarks>
public static class TitleScenarioAnalyser
{
    public static IReadOnlyList<TitleScenario> Analyse(
        ChampionshipStandings standings,
        ChampionshipForecast forecast,
        int racesCompleted,
        int topN)
    {
        ArgumentNullException.ThrowIfNull(standings);
        ArgumentNullException.ThrowIfNull(forecast);

        if (standings.Drivers.Count == 0)
        {
            return [];
        }

        var available = ChampionshipPoints.MaximumAvailable(
            forecast.RacesRemaining, forecast.SprintsRemaining);

        var probabilities = forecast.Drivers
            .ToDictionary(d => d.DriverNumber, d => d.Odds.TitleProbability);

        var leader = standings.Drivers[0];
        var runnerUp = standings.Drivers.Count > 1 ? standings.Drivers[1] : null;

        return
        [
            .. standings.Drivers
                .Take(topN)
                .Select(driver => Build(
                    driver,
                    leader,
                    runnerUp,
                    forecast,
                    available,
                    racesCompleted,
                    probabilities.GetValueOrDefault(driver.DriverNumber)))
        ];
    }

    private static TitleScenario Build(
        DriverStanding driver,
        DriverStanding leader,
        DriverStanding? runnerUp,
        ChampionshipForecast forecast,
        double available,
        int racesCompleted,
        double titleProbability)
    {
        var isLeader = driver.DriverNumber == leader.DriverNumber;
        var behind = leader.Points - driver.Points;
        var ceiling = driver.Points + available;

        // A tie on points goes to whoever has more wins. A challenger who has to win everything
        // left will have them, so reaching the leader's total is enough.
        var alive = isLeader || ceiling >= leader.Points;

        var clinched = isLeader && (runnerUp is null || runnerUp.Points + available < leader.Points);
        var toClinch = isLeader && runnerUp is not null
            ? Math.Max(0, runnerUp.Points + available - leader.Points + 1)
            : (double?)null;

        var leaderAverage = !isLeader && alive
            ? ChampionshipPoints.BestAverageWithin(
                ceiling - leader.Points, forecast.RacesRemaining, forecast.SprintsRemaining)
            : null;

        var requiredPerRace = !isLeader && forecast.RacesRemaining > 0
            ? SeasonAverage(leader.Points, racesCompleted) + (behind / forecast.RacesRemaining)
            : (double?)null;

        return new TitleScenario(
            driver.DriverNumber,
            driver.FullName,
            driver.NameAcronym,
            driver.TeamName,
            driver.TeamColour,
            driver.Position,
            driver.Points,
            behind,
            available,
            ceiling,
            isLeader,
            alive,
            clinched,
            clinched ? 0 : toClinch,
            leaderAverage,
            requiredPerRace,
            requiredPerRace is { } required ? ChampionshipPoints.RacePositionWorth(required) : null,
            titleProbability,
            Describe(driver, leader, runnerUp, forecast, behind, available, alive, clinched, toClinch, leaderAverage));
    }

    private static double SeasonAverage(double points, int racesCompleted) =>
        racesCompleted > 0 ? points / racesCompleted : 0;

    private static string Describe(
        DriverStanding driver,
        DriverStanding leader,
        DriverStanding? runnerUp,
        ChampionshipForecast forecast,
        double behind,
        double available,
        bool alive,
        bool clinched,
        double? toClinch,
        int? leaderAverage)
    {
        if (clinched)
        {
            return $"Champion already. {driver.NameAcronym} cannot be caught with only " +
                   $"{Number(available)} points left to play for.";
        }

        if (driver.DriverNumber == leader.DriverNumber)
        {
            return runnerUp is null
                ? $"Leads the championship with {Number(available)} points still available."
                : $"Leads {runnerUp.NameAcronym} by {Number(leader.Points - runnerUp.Points)} points. " +
                  $"Another {Number(toClinch ?? 0)} points settles it regardless of what anyone else does.";
        }

        if (!alive)
        {
            return $"Mathematically out — {Number(behind)} points behind with only " +
                   $"{Number(available)} still available.";
        }

        // Second is the best anyone can manage behind a contender who wins everything, so a
        // requirement of "no better than 2nd" is really no requirement at all.
        if (leaderAverage == 2)
        {
            return $"Winning all {Sessions(forecast)} is enough on its own — " +
                   $"{leader.NameAcronym} could finish second every time and still lose.";
        }

        return $"Must win all {Sessions(forecast)}, with {leader.NameAcronym} " +
               $"averaging no better than {Placing(leaderAverage)} across the rest of the season.";
    }

    private static string Sessions(ChampionshipForecast forecast)
    {
        var races = $"{forecast.RacesRemaining} remaining race{(forecast.RacesRemaining == 1 ? "" : "s")}";

        return forecast.SprintsRemaining switch
        {
            0 => races,
            1 => $"{races} and the sprint",
            2 => $"{races} and both sprints",
            var many => $"{races} and all {many} sprints"
        };
    }

    private static string Placing(int? position) => position switch
    {
        null => "any finish",
        >= 11 => "outside the points",
        _ => Ordinal(position.Value)
    };

    private static string Ordinal(int value)
    {
        var suffix = (value % 100) is >= 11 and <= 13
            ? "th"
            : (value % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };

        return value.ToString(CultureInfo.InvariantCulture) + suffix;
    }

    private static string Number(double value) =>
        value.ToString(Math.Abs(value % 1) < 1e-9 ? "0" : "0.#", CultureInfo.InvariantCulture);
}
