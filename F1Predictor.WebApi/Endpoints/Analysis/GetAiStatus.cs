using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Analysis.GetAiStatus;
using F1Predictor.WebApi.Extensions;
using F1Predictor.WebApi.Infrastructure;

namespace F1Predictor.WebApi.Endpoints.Analysis;

internal sealed class GetAiStatus : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/ai/status", async (
            IQueryHandler<GetAiStatusQuery, AiStatusResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new GetAiStatusQuery(), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Analysis)
        .WithName("GetAiStatus")
        .WithSummary("Whether an AI provider is configured, and which model.")
        .Produces<AiStatusResponse>(StatusCodes.Status200OK);
    }
}
