using F1Predictor.Application.Abstractions.AI;
using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Analysis.FindSimilarRaces;
using Microsoft.Extensions.AI;
using SharedKernel;

namespace F1Predictor.Application.Features.Analysis.SearchRaces;

internal sealed class SearchRacesQueryHandler(
    IAiCapabilities ai, IEmbeddingGenerator<string, Embedding<float>> embeddings, IRaceEmbeddingIndex index, IApplicationDbContext context)
    : IQueryHandler<SearchRacesQuery, SimilarRacesResponse>
{
    public async Task<Result<SimilarRacesResponse>> Handle(SearchRacesQuery query, CancellationToken cancellationToken)
    {
        if (!ai.EmbeddingsAvailable)
        {
            return Result.Failure<SimilarRacesResponse>(AnalysisErrors.AiUnavailable);
        }

        var generated = await embeddings.GenerateAsync([query.Text], cancellationToken: cancellationToken);
        var matches = await index.SearchAsync(generated[0].Vector, query.Top, excludeSessionKey: null, cancellationToken);
        var rows = await SimilarRaceHydration.HydrateAsync(context, matches, cancellationToken);

        return Result.Success(new SimilarRacesResponse(SourceSessionKey: 0, SourceMeetingName: query.Text, rows));
    }
}
