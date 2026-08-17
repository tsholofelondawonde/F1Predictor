using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Domain.Championship;
using Microsoft.Extensions.Caching.Memory;
using SharedKernel;

namespace F1Predictor.Application.Features.Championship.GetForecast;

internal sealed class GetChampionshipForecastQueryHandler(
    IApplicationDbContext context,
    IMemoryCache cache)
    : IQueryHandler<GetChampionshipForecastQuery, ChampionshipForecastResponse>
{
    private const string MethodGuidance =
        "Each remaining session is simulated as a whole finishing order drawn from a Plackett-Luce " +
        "model of driver pace, fitted on this season's Grand Prix results with recent races weighted " +
        "more heavily, plus a per-driver retirement rate shrunk towards the field average. Points are " +
        "awarded on the real scale and the tables settled by count-back. Read these as odds, not " +
        "forecasts: one season is a thin basis for a pace estimate, and nothing here knows about " +
        "upgrades, penalties, weather or a driver's record at a particular circuit.";

    public async Task<Result<ChampionshipForecastResponse>> Handle(
        GetChampionshipForecastQuery query,
        CancellationToken cancellationToken)
    {
        var season = await SeasonChampionship.LoadAsync(context, query.Year, cancellationToken);

        if (!season.HasResults)
        {
            return Result.Failure<ChampionshipForecastResponse>(
                ChampionshipErrors.NoResults(query.Year));
        }

        if (season.SeasonComplete)
        {
            return Result.Failure<ChampionshipForecastResponse>(
                ChampionshipErrors.SeasonComplete(query.Year));
        }

        var forecast = CachedForecast.For(cache, season, ChampionshipSimulator.DefaultSimulations);

        var driverOdds = forecast.Drivers.ToDictionary(d => d.DriverNumber, d => d.Odds);
        var teamOdds = forecast.Constructors.ToDictionary(c => c.TeamName, c => c.Odds, StringComparer.Ordinal);

        var driverCeiling = ChampionshipPoints.MaximumAvailable(
            forecast.RacesRemaining, forecast.SprintsRemaining);
        var teamCeiling = ChampionshipPoints.MaximumAvailableForConstructor(
            forecast.RacesRemaining, forecast.SprintsRemaining);

        var driverLeader = season.Standings.Drivers[0].Points;
        var teamLeader = season.Standings.Constructors[0].Points;

        var response = new ChampionshipForecastResponse(
            season.Year,
            forecast.Simulations,
            forecast.RacesRemaining,
            forecast.SprintsRemaining,
            driverCeiling,
            MethodGuidance,
            [.. season.Standings.Drivers.Select(d => Describe(
                d, driverOdds[d.DriverNumber], d.Points + driverCeiling >= driverLeader))],
            [.. season.Standings.Constructors.Select(c => Describe(
                c, teamOdds[c.TeamName], c.Points + teamCeiling >= teamLeader))]);

        return Result.Success(response);
    }

    private static DriverForecastResponse Describe(DriverStanding driver, TitleOdds odds, bool alive) =>
        new(driver.Position,
            driver.DriverNumber,
            driver.FullName,
            driver.NameAcronym,
            driver.TeamName,
            driver.TeamColour,
            driver.Points,
            driver.Wins,
            driver.Podiums,
            odds.TitleProbability,
            odds.MeanFinalPoints,
            odds.LowFinalPoints,
            odds.HighFinalPoints,
            alive);

    private static ConstructorForecastResponse Describe(
        ConstructorStanding team,
        TitleOdds odds,
        bool alive) =>
        new(team.Position,
            team.TeamName,
            team.TeamColour,
            team.Points,
            team.Wins,
            team.Podiums,
            odds.TitleProbability,
            odds.MeanFinalPoints,
            odds.LowFinalPoints,
            odds.HighFinalPoints,
            alive);
}
