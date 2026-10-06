using F1Predictor.Application.Abstractions.AI;
using F1Predictor.Application.Abstractions.Messaging;
using SharedKernel;

namespace F1Predictor.Application.Features.Analysis.GetAiStatus;

internal sealed class GetAiStatusQueryHandler(IAiCapabilities ai) : IQueryHandler<GetAiStatusQuery, AiStatusResponse>
{
    public Task<Result<AiStatusResponse>> Handle(GetAiStatusQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(new AiStatusResponse(
            ai.ChatAvailable, ai.EmbeddingsAvailable, ai.Provider, ai.Model, ai.EmbeddingModel)));
}
