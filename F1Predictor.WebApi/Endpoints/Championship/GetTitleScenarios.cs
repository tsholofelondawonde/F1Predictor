using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Championship.GetScenarios;
using F1Predictor.WebApi.Extensions;
using F1Predictor.WebApi.Infrastructure;

namespace F1Predictor.WebApi.Endpoints.Championship;

internal sealed class GetTitleScenarios : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/seasons/{year:int}/title-scenarios", async (
            int year,
            int? topN,
            IQueryHandler<GetTitleScenariosQuery, TitleScenariosResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetTitleScenariosQuery(year, topN ?? 5);

            var result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Championship)
        .WithName("GetTitleScenarios")
        .WithSummary("What each leading contender needs to win the drivers' title.")
        .WithDescription(
            "Pure championship arithmetic rather than simulation: who is still mathematically " +
            "alive, what the leader needs to seal it, and — assuming a contender wins every " +
            "remaining session — the best average finish the leader could still manage and lose. " +
            "Each contender also carries their simulated title probability for comparison.")
        .Produces<TitleScenariosResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
