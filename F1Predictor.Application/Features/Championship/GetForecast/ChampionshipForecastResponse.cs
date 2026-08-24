namespace F1Predictor.Application.Features.Championship.GetForecast;

/// <param name="Year">Season being forecast.</param>
/// <param name="Simulations">Seasons simulated to produce these probabilities.</param>
/// <param name="RacesRemaining">Grands Prix still to run.</param>
/// <param name="SprintsRemaining">Sprints still to run.</param>
/// <param name="PointsStillAvailable">The most any one entrant can still score.</param>
/// <param name="Method">How the numbers were produced, and what they are worth.</param>
/// <param name="Drivers">Drivers' title odds, championship order.</param>
/// <param name="Constructors">Constructors' title odds, championship order.</param>
public sealed record ChampionshipForecastResponse(
    int Year,
    int Simulations,
    int RacesRemaining,
    int SprintsRemaining,
    double PointsStillAvailable,
    string Method,
    IReadOnlyList<DriverForecastResponse> Drivers,
    IReadOnlyList<ConstructorForecastResponse> Constructors);

/// <param name="Position">Current championship position.</param>
/// <param name="DriverNumber">Car number.</param>
/// <param name="FullName">Driver's full name.</param>
/// <param name="NameAcronym">Three-letter abbreviation.</param>
/// <param name="TeamName">Constructor.</param>
/// <param name="TeamColour">Six-digit hex team colour, no leading hash.</param>
/// <param name="Points">Points scored so far.</param>
/// <param name="Wins">Grand Prix wins.</param>
/// <param name="Podiums">Grand Prix podiums.</param>
/// <param name="TitleProbability">Share of simulated seasons this driver won the title, 0 to 1.</param>
/// <param name="ProjectedPoints">Average final points across simulations.</param>
/// <param name="ProjectedPointsLow">10th percentile of final points.</param>
/// <param name="ProjectedPointsHigh">90th percentile of final points.</param>
/// <param name="IsMathematicallyAlive">Whether any sequence of results still gives them the title.</param>
public sealed record DriverForecastResponse(
    int Position,
    int DriverNumber,
    string FullName,
    string NameAcronym,
    string TeamName,
    string TeamColour,
    double Points,
    int Wins,
    int Podiums,
    double TitleProbability,
    double ProjectedPoints,
    double ProjectedPointsLow,
    double ProjectedPointsHigh,
    bool IsMathematicallyAlive);

/// <param name="Position">Current championship position.</param>
/// <param name="TeamName">Constructor name.</param>
/// <param name="TeamColour">Six-digit hex team colour, no leading hash.</param>
/// <param name="Points">Points scored so far.</param>
/// <param name="Wins">Combined Grand Prix wins.</param>
/// <param name="Podiums">Combined Grand Prix podiums.</param>
/// <param name="TitleProbability">Share of simulated seasons this constructor won the title, 0 to 1.</param>
/// <param name="ProjectedPoints">Average final points across simulations.</param>
/// <param name="ProjectedPointsLow">10th percentile of final points.</param>
/// <param name="ProjectedPointsHigh">90th percentile of final points.</param>
/// <param name="IsMathematicallyAlive">Whether any sequence of results still gives them the title.</param>
public sealed record ConstructorForecastResponse(
    int Position,
    string TeamName,
    string TeamColour,
    double Points,
    int Wins,
    int Podiums,
    double TitleProbability,
    double ProjectedPoints,
    double ProjectedPointsLow,
    double ProjectedPointsHigh,
    bool IsMathematicallyAlive);
