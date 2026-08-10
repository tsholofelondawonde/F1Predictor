using F1Predictor.Application.Abstractions.Legacy;
using F1Predictor.Application.Abstractions.Messaging;

namespace F1Predictor.Application.Features.Legacy.Import;

/// <summary>
/// Copies the original console prototype's SQLite database into the application database.
/// </summary>
public sealed class ImportLegacyDatabaseCommand : ICommand<LegacyImportCounts>
{
    /// <summary>Path to the legacy <c>f1predictor.db</c> file.</summary>
    public string SqlitePath { get; set; } = "";
}
