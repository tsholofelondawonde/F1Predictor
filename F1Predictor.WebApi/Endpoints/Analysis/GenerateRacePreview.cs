using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Analysis.GenerateRacePreview;
using F1Predictor.WebApi.Extensions;
using F1Predictor.WebApi.Infrastructure;

namespace F1Predictor.WebApi.Endpoints.Analysis;

internal sealed class GenerateRacePreview : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/races/{sessionKey:int}/preview", async (
            int sessionKey,
            ICommandHandler<GenerateRacePreviewCommand, RacePreviewResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new GenerateRacePreviewCommand { SessionKey = sessionKey }, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Analysis)
        .WithName("GenerateRacePreview")
        .WithSummary("Writes (or rewrites) an AI preview of the next Grand Prix from the model's numbers.")
        .Produces<RacePreviewResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireApiKey()
        .RequireRateLimiting(RateLimiterPolicies.Mutating);
    }
}
