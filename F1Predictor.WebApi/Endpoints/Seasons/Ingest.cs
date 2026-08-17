using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Seasons.Ingest;
using F1Predictor.WebApi.Extensions;
using F1Predictor.WebApi.Infrastructure;

namespace F1Predictor.WebApi.Endpoints.Seasons;

internal sealed class Ingest : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/seasons/{year:int}/ingest", async (
            int year,
            bool? force,
            ICommandHandler<IngestSeasonCommand, IngestSeasonResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new IngestSeasonCommand { Year = year, Force = force ?? false };

            var result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Seasons)
        .WithName("IngestSeason")
        .WithSummary("Ingests a season of race data from OpenF1.")
        .WithDescription(
            "Fetches every points-scoring session of the season — Grand Prix and sprint alike — " +
            "and persists the raw grid, result, pit stop, weather and entry-list rows. Races that " +
            "have not run yet are recorded without results so the next race is known in advance. " +
            "Requests are deliberately spaced out to stay polite to the free OpenF1 API, so a full " +
            "season takes on the order of two minutes. Safe to re-run: sessions already stored with " +
            "results are skipped, unless force=true replaces them.")
        .Produces<IngestSeasonResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
