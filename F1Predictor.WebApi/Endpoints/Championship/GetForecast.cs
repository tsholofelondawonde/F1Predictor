using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Championship.GetForecast;
using F1Predictor.WebApi.Extensions;
using F1Predictor.WebApi.Infrastructure;

namespace F1Predictor.WebApi.Endpoints.Championship;

internal sealed class GetForecast : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/seasons/{year:int}/championship-forecast", async (
            int year,
            IQueryHandler<GetChampionshipForecastQuery, ChampionshipForecastResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(
                new GetChampionshipForecastQuery(year), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Championship)
        .WithName("GetChampionshipForecast")
        .WithSummary("Title odds for every driver and constructor.")
        .WithDescription(
            "Simulates the remaining calendar ten thousand times, drawing each finishing order " +
            "from a Plackett-Luce model of driver pace fitted on this season, and reports how " +
            "often each entrant ends up on top. The seed is fixed, so repeated calls return the " +
            "same numbers. See the 'method' field in the response for what the odds are worth.")
        .Produces<ChampionshipForecastResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
