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
            ICommandHandler<IngestSeasonCommand, IngestSeasonResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new IngestSeasonCommand { Year = year };

            var result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Seasons)
        .WithName("IngestSeason")
        .WithSummary("Ingests a season of race data from OpenF1.")
        .WithDescription(
            "Fetches every race weekend of the season and persists the raw grid, result, pit " +
            "stop and weather rows. Requests are deliberately spaced out to stay polite to the " +
            "free OpenF1 API, so a full season takes on the order of one to two minutes. " +
            "Safe to re-run: weekends already ingested are skipped.")
        .Produces<IngestSeasonResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
