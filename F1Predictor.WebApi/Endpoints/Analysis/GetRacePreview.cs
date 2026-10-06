using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Analysis.GenerateRacePreview;
using F1Predictor.Application.Features.Analysis.GetRacePreview;
using F1Predictor.WebApi.Extensions;
using F1Predictor.WebApi.Infrastructure;

namespace F1Predictor.WebApi.Endpoints.Analysis;

internal sealed class GetRacePreview : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/races/{sessionKey:int}/preview", async (
            int sessionKey,
            IQueryHandler<GetRacePreviewQuery, RacePreviewResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new GetRacePreviewQuery(sessionKey), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Analysis)
        .WithName("GetRacePreview")
        .WithSummary("Returns the last generated AI preview for a race session, if one exists.")
        .Produces<RacePreviewResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
