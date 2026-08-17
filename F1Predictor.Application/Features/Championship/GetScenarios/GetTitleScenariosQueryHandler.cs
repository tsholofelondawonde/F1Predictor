using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Domain.Championship;
using Microsoft.Extensions.Caching.Memory;
using SharedKernel;

namespace F1Predictor.Application.Features.Championship.GetScenarios;

internal sealed class GetTitleScenariosQueryHandler(
    IApplicationDbContext context,
    IMemoryCache cache)
    : IQueryHandler<GetTitleScenariosQuery, TitleScenariosResponse>
{
    public async Task<Result<TitleScenariosResponse>> Handle(
        GetTitleScenariosQuery query,
        CancellationToken cancellationToken)
    {
        var season = await SeasonChampionship.LoadAsync(context, query.Year, cancellationToken);

        if (!season.HasResults)
        {
            return Result.Failure<TitleScenariosResponse>(ChampionshipErrors.NoResults(query.Year));
        }

        if (season.SeasonComplete)
        {
            return Result.Failure<TitleScenariosResponse>(ChampionshipErrors.SeasonComplete(query.Year));
        }

        var forecast = CachedForecast.For(cache, season, ChampionshipSimulator.DefaultSimulations);

        var scenarios = TitleScenarioAnalyser.Analyse(
            season.Standings, forecast, season.RacesCompleted, query.TopN);

        var response = new TitleScenariosResponse(
            season.Year,
            forecast.RacesRemaining,
            forecast.SprintsRemaining,
            ChampionshipPoints.MaximumAvailable(forecast.RacesRemaining, forecast.SprintsRemaining),
            season.Standings.Drivers[0].FullName,
            [.. scenarios.Select(s => new TitleScenarioResponse(
                s.Position,
                s.DriverNumber,
                s.FullName,
                s.NameAcronym,
                s.TeamName,
                s.TeamColour,
                s.Points,
                s.PointsBehindLeader,
                s.MaximumPossiblePoints,
                s.IsLeader,
                s.IsMathematicallyAlive,
                s.HasClinchedTitle,
                s.PointsToClinch,
                s.LeaderMustAverageNoBetterThan,
                s.RequiredPointsPerRace,
                s.RequiredAveragePosition,
                s.TitleProbability,
                s.Requirement))]);

        return Result.Success(response);
    }
}
