using F1Predictor.Application.Abstractions.AI;
using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Abstractions.Messaging;
using SharedKernel;

namespace F1Predictor.Application.Features.Analysis.FindSimilarRaces;

internal sealed class FindSimilarRacesQueryHandler(IAiCapabilities ai, IApplicationDbContext context, IRaceEmbeddingIndex index)
    : IQueryHandler<FindSimilarRacesQuery, SimilarRacesResponse>
{
    public async Task<Result<SimilarRacesResponse>> Handle(FindSimilarRacesQuery query, CancellationToken cancellationToken)
    {
        // Gated like SearchRaces, so pausing the AI layer (Ai:Enabled=false) switches off every
        // embeddings-backed route, not just the ones that call the provider.
        if (!ai.EmbeddingsAvailable)
        {
            return Result.Failure<SimilarRacesResponse>(AnalysisErrors.AiUnavailable);
        }

        // No embedding call — SearchLikeAsync reuses the source race's stored vector, so this
        // works even when the embedding provider is currently down or misconfigured.
        var matches = await index.SearchLikeAsync(query.SessionKey, query.Top, cancellationToken);
        if (matches.Count == 0)
        {
            // Empty could mean "not indexed" or "indexed, but nothing else to compare against
            // yet" — only the former is an error.
            var indexed = await index.GetAsync(query.SessionKey, cancellationToken);
            if (indexed is null)
            {
                return Result.Failure<SimilarRacesResponse>(AnalysisErrors.NotIndexed(query.SessionKey));
            }
        }

        var meetingName = await SimilarRaceHydration.MeetingNameFor(context, query.SessionKey, cancellationToken);
        var rows = await SimilarRaceHydration.HydrateAsync(context, matches, cancellationToken);

        return Result.Success(new SimilarRacesResponse(query.SessionKey, meetingName, rows));
    }
}
