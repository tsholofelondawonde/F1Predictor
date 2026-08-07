using F1Predictor.Application.Abstractions.Legacy;
using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Legacy.Import;
using F1Predictor.WebApi.Extensions;
using F1Predictor.WebApi.Infrastructure;

namespace F1Predictor.WebApi.Endpoints.Admin;

/// <summary>
/// One-off migration of the original console prototype's SQLite file into Postgres.
/// </summary>
/// <remarks>
/// Development only, and temporary: once the data is across, this endpoint, the
/// <see cref="ILegacyDatabaseImporter"/> implementation and the SQLite package reference can
/// all be deleted.
/// </remarks>
internal sealed class ImportLegacyDatabase : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var environment = app.ServiceProvider.GetRequiredService<IHostEnvironment>();

        if (!environment.IsDevelopment())
        {
            return;
        }

        app.MapPost("/api/admin/import-legacy-sqlite", async (
            ImportLegacyDatabaseRequest request,
            ICommandHandler<ImportLegacyDatabaseCommand, LegacyImportCounts> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ImportLegacyDatabaseCommand { SqlitePath = request.SqlitePath };

            var result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Admin)
        .WithName("ImportLegacyDatabase")
        .WithSummary("Imports the legacy SQLite database (Development only).")
        .WithDescription(
            "Copies the console prototype's f1predictor.db into the application database so " +
            "already-ingested race data does not have to be fetched from OpenF1 again. " +
            "Idempotent: race sessions already present are skipped.")
        .Produces<LegacyImportCounts>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

/// <param name="SqlitePath">Absolute path to the legacy <c>f1predictor.db</c> file.</param>
internal sealed record ImportLegacyDatabaseRequest(string SqlitePath);
