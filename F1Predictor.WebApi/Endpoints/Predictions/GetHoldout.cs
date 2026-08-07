using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Predictions.GetHoldout;
using F1Predictor.WebApi.Extensions;
using F1Predictor.WebApi.Infrastructure;

namespace F1Predictor.WebApi.Endpoints.Predictions;

internal sealed class GetHoldout : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/seasons/{year:int}/holdout", async (
            int year,
            IQueryHandler<GetHoldoutPredictionsQuery, HoldoutPredictionsResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new GetHoldoutPredictionsQuery(year), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Predictions)
        .WithName("GetHoldoutPredictions")
        .WithSummary("Predictions for the held-out race, next to what actually happened.")
        .WithDescription(
            "The most recent race of the season is excluded from training. This returns both " +
            "models' probabilities for every driver in it alongside their real grid and finish " +
            "positions — the check on whether the models learned anything real.")
        .Produces<HoldoutPredictionsResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
