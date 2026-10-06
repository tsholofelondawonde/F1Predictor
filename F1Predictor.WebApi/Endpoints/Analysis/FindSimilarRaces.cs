using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Analysis.FindSimilarRaces;
using F1Predictor.WebApi.Extensions;
using F1Predictor.WebApi.Infrastructure;

namespace F1Predictor.WebApi.Endpoints.Analysis;

internal sealed class FindSimilarRaces : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/races/{sessionKey:int}/similar", async (
            int sessionKey,
            int? top,
            IQueryHandler<FindSimilarRacesQuery, SimilarRacesResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var clampedTop = Math.Clamp(top ?? 5, 1, 10);

            var result = await handler.Handle(new FindSimilarRacesQuery(sessionKey, clampedTop), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Analysis)
        .WithName("FindSimilarRaces")
        .WithSummary("Past races most similar to one already-indexed race, by recorded facts.")
        .WithDescription(
            "Similarity is cosine distance over an embedding of each race's deterministic fact " +
            "sheet (winner, weather, retirements, pit stops, ...), never over prose, so it works " +
            "regardless of whether an AI provider is configured for narration. Reuses the source " +
            "race's already-stored vector rather than making a fresh embedding call.")
        .Produces<SimilarRacesResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
