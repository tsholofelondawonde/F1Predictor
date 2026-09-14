using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Seasons.GetDataStatus;
using F1Predictor.WebApi.Extensions;
using F1Predictor.WebApi.Infrastructure;

namespace F1Predictor.WebApi.Endpoints.Seasons;

internal sealed class GetDataStatus : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/seasons/{year:int}/data-status", async (
            int year,
            IQueryHandler<GetDataStatusQuery, DataStatusResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new GetDataStatusQuery(year), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Seasons)
        .WithName("GetDataStatus")
        .WithSummary("Reports whether a season's standings and predictions are current.")
        .WithDescription("True IsStale means a race has happened but hasn't been ingested, features haven't been rebuilt since, or the models have never been trained.")
        .Produces<DataStatusResponse>(StatusCodes.Status200OK);
    }
}
