using F1Predictor.Application.Abstractions.Legacy;
using F1Predictor.Application.Abstractions.Messaging;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace F1Predictor.Application.Features.Legacy.Import;

internal sealed class ImportLegacyDatabaseCommandHandler(
    ILegacyDatabaseImporter importer,
    ILogger<ImportLegacyDatabaseCommandHandler> logger)
    : ICommandHandler<ImportLegacyDatabaseCommand, LegacyImportCounts>
{
    public async Task<Result<LegacyImportCounts>> Handle(
        ImportLegacyDatabaseCommand command,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(command.SqlitePath))
        {
            return Result.Failure<LegacyImportCounts>(Error.NotFound(
                "LegacyImport.FileNotFound",
                $"No SQLite database found at '{command.SqlitePath}'.",
                "The legacy database file could not be found at that path."));
        }

        var counts = await importer.ImportAsync(command.SqlitePath, cancellationToken);

        logger.LogInformation(
            "Legacy import copied {Meetings} meetings, {Sessions} sessions and {Features} feature rows ({Skipped} sessions already present).",
            counts.Meetings, counts.RaceSessions, counts.Features, counts.SessionsSkipped);

        return Result.Success(counts);
    }
}
