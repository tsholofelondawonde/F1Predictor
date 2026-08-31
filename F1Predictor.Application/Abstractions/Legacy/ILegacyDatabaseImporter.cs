namespace F1Predictor.Application.Abstractions.Legacy;

/// <summary>
/// One-off migration path for the SQLite database the original console prototype wrote.
/// </summary>
/// <remarks>
/// This exists only to carry already-ingested race data across to Postgres without hitting
/// the free OpenF1 API again. Once the data is across it can be deleted along with its
/// implementation and the SQLite package reference.
/// </remarks>
public interface ILegacyDatabaseImporter
{
    /// <summary>
    /// Copies every table from the SQLite file at <paramref name="sqlitePath"/>. Race weekends
    /// already present in the target are skipped, so running it twice is safe.
    /// </summary>
    Task<LegacyImportCounts> ImportAsync(string sqlitePath, CancellationToken cancellationToken);
}

/// <param name="Meetings">Race weekends copied.</param>
/// <param name="RaceSessions">Race sessions copied.</param>
/// <param name="GridEntries">Starting grid rows copied.</param>
/// <param name="Results">Classified result rows copied.</param>
/// <param name="PitStops">Pit stop rows copied.</param>
/// <param name="WeatherReadings">Weather samples copied.</param>
/// <param name="Features">Engineered feature rows copied.</param>
/// <param name="SessionsSkipped">Sessions already present in the target and therefore left alone.</param>
public sealed record LegacyImportCounts(
    int Meetings,
    int RaceSessions,
    int GridEntries,
    int Results,
    int PitStops,
    int WeatherReadings,
    int Features,
    int SessionsSkipped);
