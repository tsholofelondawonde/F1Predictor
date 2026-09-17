using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Analysis.ExplainDriverPrediction;
using F1Predictor.WebApi.Extensions;
using F1Predictor.WebApi.Infrastructure;

namespace F1Predictor.WebApi.Endpoints.Analysis;

public sealed class ExplainDriverPrediction : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/seasons/{year:int}/next-race/drivers/{driverNumber:int}/explanation", async (
            int year,
            int driverNumber,
            IQueryHandler<ExplainDriverPredictionQuery, DriverExplanationResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new ExplainDriverPredictionQuery(year, driverNumber), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Analysis)
        .WithName("ExplainDriverPrediction")
        .WithSummary("Why the model rates one driver the way it does for the next Grand Prix.")
        .WithDescription(
            "Per-feature contributions computed from the saved model itself. When an AI provider " +
            "is configured a short narrative of those numbers is added; otherwise 'narrative' is null.")
        .Produces<DriverExplanationResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
