using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Analysis.ExplainDriverPrediction;
using F1Predictor.Application.Features.Analysis.ExplainRacePrediction;
using F1Predictor.WebApi.Extensions;
using F1Predictor.WebApi.Infrastructure;

namespace F1Predictor.WebApi.Endpoints.Analysis;

internal sealed class ExplainRacePrediction : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/races/{sessionKey:int}/predictions/{driverNumber:int}/explanation", async (
            int sessionKey,
            int driverNumber,
            IQueryHandler<ExplainRacePredictionQuery, DriverExplanationResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new ExplainRacePredictionQuery(sessionKey, driverNumber), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Analysis)
        .WithName("ExplainRacePrediction")
        .WithSummary("Why the model rated one driver the way it did, and how the race actually went.")
        .WithDescription(
            "Per-feature contributions computed from the saved model itself, plus the driver's actual " +
            "finish for a classified race. When an AI provider is configured a short narrative comparing " +
            "the prediction with the result is added; otherwise 'narrative' is null.")
        .Produces<DriverExplanationResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
