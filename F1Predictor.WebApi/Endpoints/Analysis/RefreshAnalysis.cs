using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Analysis.RefreshAnalysis;
using F1Predictor.WebApi.Extensions;
using F1Predictor.WebApi.Infrastructure;

namespace F1Predictor.WebApi.Endpoints.Analysis;

/// <summary>
/// Manual escape hatch for the same refresh the Infrastructure layer's Quartz-scheduled
/// AnalysisRefreshJob runs automatically — lets an operator force a preview refresh (or a
/// single season's) without waiting for the next ingest or the interval backstop.
/// </summary>
internal sealed class RefreshAnalysis : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/analysis/refresh", async (
            int? year,
            ICommandHandler<RefreshAnalysisCommand, RefreshAnalysisResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new RefreshAnalysisCommand { Year = year }, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Analysis)
        .WithName("RefreshAnalysis")
        .WithSummary("Regenerates any stale or missing AI race previews.")
        .WithDescription(
            "Refreshes the given season's next-race preview, or every ingested season's when no " +
            "year is given. Skips a preview that is already fresh. This is the same refresh " +
            "AnalysisRefreshJob runs automatically after ingestion and on an interval backstop — " +
            "this route exists to force it on demand.")
        .Produces<RefreshAnalysisResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .RequireApiKey()
        .RequireRateLimiting(RateLimiterPolicies.Mutating);
    }
}
