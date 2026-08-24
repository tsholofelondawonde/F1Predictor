using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Seasons.GetRaces;
using F1Predictor.WebApi.Extensions;
using F1Predictor.WebApi.Infrastructure;

namespace F1Predictor.WebApi.Endpoints.Seasons;

internal sealed class GetRaces : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/seasons/{year:int}/races", async (
            int year,
            IQueryHandler<GetSeasonRacesQuery, IReadOnlyList<SeasonRaceResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new GetSeasonRacesQuery(year), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Seasons)
        .WithName("GetSeasonRaces")
        .WithSummary("Lists the ingested races of a season.")
        .WithDescription("Returns each race with its session key, so a caller can request predictions for one.")
        .Produces<IReadOnlyList<SeasonRaceResponse>>(StatusCodes.Status200OK);
    }
}
