namespace F1Predictor.Infrastructure.Ingestion;

/// <summary>
/// Settings for <see cref="SeasonIngestionCoordinatorJob"/>, bound from the
/// "IngestionScheduler" configuration section.
/// </summary>
internal sealed class IngestionSchedulerOptions
{
    public const string SectionName = "IngestionScheduler";

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// How long after a session's <c>DateStart</c> to treat it as "should be over" before the
    /// coordinator re-checks it — covers race/sprint duration plus OpenF1's publishing lag.
    /// </summary>
    public int PostSessionBufferMinutes { get; set; } = 180;

    /// <summary>
    /// How often the coordinator job ticks. Each tick is a local database query; it only calls
    /// OpenF1 when a session is actually due, so this can be fairly frequent without hammering
    /// the API.
    /// </summary>
    public int CoordinatorIntervalMinutes { get; set; } = 30;
}
