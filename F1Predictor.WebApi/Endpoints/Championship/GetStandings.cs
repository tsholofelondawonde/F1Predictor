using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Championship.GetStandings;
using F1Predictor.WebApi.Extensions;
using F1Predictor.WebApi.Infrastructure;

namespace F1Predictor.WebApi.Endpoints.Championship;

internal sealed class GetStandings : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/seasons/{year:int}/standings", async (
            int year,
            IQueryHandler<GetStandingsQuery, StandingsResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new GetStandingsQuery(year), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Championship)
        .WithName("GetStandings")
        .WithSummary("Drivers' and constructors' championship tables as they stand.")
        .WithDescription(
            "Points come from OpenF1's own award for each result, so sprint scoring is included " +
            "automatically. Win and podium counts refer to Grands Prix only, and entrants level " +
            "on points are separated by the count-back on finishing positions.")
        .Produces<StandingsResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
