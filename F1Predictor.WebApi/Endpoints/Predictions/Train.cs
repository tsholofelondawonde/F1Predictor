using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Predictions.Train;
using F1Predictor.WebApi.Extensions;
using F1Predictor.WebApi.Infrastructure;

namespace F1Predictor.WebApi.Endpoints.Predictions;

internal sealed class Train : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/models/train", async (
            int year,
            int? fromYear,
            string? notes,
            ICommandHandler<TrainModelsCommand, TrainModelsResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new TrainModelsCommand { Year = year, FromYear = fromYear, Notes = notes };

            var result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Predictions)
        .WithName("TrainModels")
        .WithSummary("Trains the podium and points-finish models on one or more seasons.")
        .WithDescription(
            "Fits two binary classifiers on the feature rows of ?fromYear (default: year) " +
            "through ?year, excluding the most recent race of year so it remains an honest " +
            "holdout. The remaining races are split by date: the latest ~20% validate, the rest " +
            "train, and no race is on both sides. Compare the returned validation AUC with " +
            "BaselineAuc (grid order alone). Accuracy isn't reported: the positive classes are " +
            "small minorities, so it flatters a model that says \"no\" every time. Every run is " +
            "recorded, with optional ?notes, at GET /api/models/runs.")
        .Produces<TrainModelsResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .RequireApiKey()
        .RequireRateLimiting(RateLimiterPolicies.Mutating);
    }
}
