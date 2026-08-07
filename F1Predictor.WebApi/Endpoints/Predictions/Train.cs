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
            ICommandHandler<TrainModelsCommand, TrainModelsResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new TrainModelsCommand { Year = year };

            var result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Predictions)
        .WithName("TrainModels")
        .WithSummary("Trains the podium and points-finish models for a season.")
        .WithDescription(
            "Fits two binary classifiers on the season's feature rows, excluding the most " +
            "recent race so it remains an honest holdout. Judge the result by the returned AUC " +
            "and F1, not accuracy — the positive classes are small minorities, so accuracy " +
            "flatters a model that predicts \"no\" every time.")
        .Produces<TrainModelsResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
