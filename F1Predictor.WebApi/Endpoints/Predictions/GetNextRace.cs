using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Predictions.PreviewNextRace;
using F1Predictor.WebApi.Extensions;
using F1Predictor.WebApi.Infrastructure;

namespace F1Predictor.WebApi.Endpoints.Predictions;

internal sealed class GetNextRace : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/seasons/{year:int}/next-race", async (
            int year,
            IQueryHandler<PreviewNextRaceQuery, NextRacePreviewResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new PreviewNextRaceQuery(year), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Predictions)
        .WithName("GetNextRacePreview")
        .WithSummary("Podium and points probabilities for the next Grand Prix.")
        .WithDescription(
            "Once qualifying has run these are predicted from the real starting grid and " +
            "'gridConfirmed' is true. Before then the grid is projected from each driver's recent " +
            "form and the probabilities should be read as provisional. Sprints are not previewed: " +
            "the models were fitted on Grands Prix only.")
        .Produces<NextRacePreviewResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
