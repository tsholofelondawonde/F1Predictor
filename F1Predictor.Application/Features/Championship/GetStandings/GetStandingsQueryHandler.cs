using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Domain.Championship;
using SharedKernel;

namespace F1Predictor.Application.Features.Championship.GetStandings;

internal sealed class GetStandingsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetStandingsQuery, StandingsResponse>
{
    public async Task<Result<StandingsResponse>> Handle(
        GetStandingsQuery query,
        CancellationToken cancellationToken)
    {
        var season = await SeasonChampionship.LoadAsync(context, query.Year, cancellationToken);

        if (!season.HasResults)
        {
            return Result.Failure<StandingsResponse>(ChampionshipErrors.NoResults(query.Year));
        }

        var racesRemaining = season.Remaining.Count(s => !s.IsSprint);
        var sprintsRemaining = season.Remaining.Count - racesRemaining;

        var response = new StandingsResponse(
            season.Year,
            season.RacesCompleted,
            season.SprintsCompleted,
            racesRemaining,
            sprintsRemaining,
            ChampionshipPoints.MaximumAvailable(racesRemaining, sprintsRemaining),
            [.. season.Standings.Drivers.Select(d => new DriverStandingResponse(
                d.Position,
                d.DriverNumber,
                d.FullName,
                d.NameAcronym,
                d.TeamName,
                d.TeamColour,
                d.Points,
                d.Wins,
                d.Podiums))],
            [.. season.Standings.Constructors.Select(c => new ConstructorStandingResponse(
                c.Position,
                c.TeamName,
                c.TeamColour,
                c.Points,
                c.Wins,
                c.Podiums,
                c.DriverNumbers))]);

        return Result.Success(response);
    }
}
