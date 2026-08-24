using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Predictions.RebuildFeatures;
using F1Predictor.WebApi.Extensions;
using F1Predictor.WebApi.Infrastructure;

namespace F1Predictor.WebApi.Endpoints.Predictions;

internal sealed class RebuildFeatures : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/features/rebuild", async (
            ICommandHandler<RebuildFeaturesCommand, RebuildFeaturesResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new RebuildFeaturesCommand(), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Predictions)
        .WithName("RebuildFeatures")
        .WithSummary("Regenerates the feature table from the raw race data.")
        .WithDescription(
            "Clears and rebuilds every driver-race feature row. A full rebuild rather than an " +
            "incremental update, so changes to the engineering rules always take effect and " +
            "stale rows cannot survive.")
        .Produces<RebuildFeaturesResponse>(StatusCodes.Status200OK)
        .RequireApiKey()
        .RequireRateLimiting(RateLimiterPolicies.Mutating);
    }
}
