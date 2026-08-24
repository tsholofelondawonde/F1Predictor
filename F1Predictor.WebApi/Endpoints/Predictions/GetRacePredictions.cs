using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Predictions.PredictRace;
using F1Predictor.WebApi.Extensions;
using F1Predictor.WebApi.Infrastructure;

namespace F1Predictor.WebApi.Endpoints.Predictions;

internal sealed class GetRacePredictions : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/races/{sessionKey:int}/predictions", async (
            int sessionKey,
            IQueryHandler<PredictRaceQuery, RacePredictionsResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new PredictRaceQuery(sessionKey), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Predictions)
        .WithName("GetRacePredictions")
        .WithSummary("Podium and points probabilities for every driver in one race.")
        .WithDescription(
            "Ordered by predicted podium probability, highest first. Note that races included " +
            "in training will flatter the model; use the holdout endpoint to judge it.")
        .Produces<RacePredictionsResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
