using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Predictions.GetTrainingRuns;
using F1Predictor.WebApi.Extensions;
using F1Predictor.WebApi.Infrastructure;

namespace F1Predictor.WebApi.Endpoints.Predictions;

internal sealed class GetTrainingRuns : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/models/runs", async (
            string? target,
            int? take,
            IQueryHandler<GetTrainingRunsQuery, TrainingRunsResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new GetTrainingRunsQuery(target, take ?? 50), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Predictions)
        .WithName("GetTrainingRuns")
        .WithSummary("The training-run log: every run's metrics, newest first.")
        .WithDescription(
            "Each call to POST /api/models/train appends one row per target: the season window, " +
            "trainer, features, validation metrics, the grid-order baseline and the holdout " +
            "race's scores, plus any notes. Compare runs here to see whether a change helped. " +
            "Optional ?target=Podium|PointsFinish and ?take= (default 50, max 200).")
        .Produces<TrainingRunsResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
