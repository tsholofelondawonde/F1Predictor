namespace F1Predictor.Domain.Championship;

/// <param name="SessionKey">OpenF1 session key of a session still to be run.</param>
/// <param name="IsSprint">Whether it scores on the sprint scale.</param>
public sealed record RemainingSession(int SessionKey, bool IsSprint);

/// <param name="TitleProbability">Share of simulated seasons this entrant finished top of the table.</param>
/// <param name="MeanFinalPoints">Average points at the end of the season across simulations.</param>
/// <param name="LowFinalPoints">10th percentile of final points — a bad run of luck.</param>
/// <param name="HighFinalPoints">90th percentile of final points — a good one.</param>
public sealed record TitleOdds(
    double TitleProbability,
    double MeanFinalPoints,
    double LowFinalPoints,
    double HighFinalPoints);

/// <param name="DriverNumber">Car number.</param>
/// <param name="Odds">Simulated outcome for this driver.</param>
public sealed record DriverTitleOdds(int DriverNumber, TitleOdds Odds);

/// <param name="TeamName">Constructor name.</param>
/// <param name="Odds">Simulated outcome for this constructor.</param>
public sealed record ConstructorTitleOdds(string TeamName, TitleOdds Odds);

/// <param name="Simulations">How many seasons were simulated.</param>
/// <param name="RacesRemaining">Grands Prix still to run.</param>
/// <param name="SprintsRemaining">Sprints still to run.</param>
/// <param name="Drivers">Per-driver odds, in the order the drivers' table was given.</param>
/// <param name="Constructors">Per-constructor odds, in the order the constructors' table was given.</param>
public sealed record ChampionshipForecast(
    int Simulations,
    int RacesRemaining,
    int SprintsRemaining,
    IReadOnlyList<DriverTitleOdds> Drivers,
    IReadOnlyList<ConstructorTitleOdds> Constructors);
