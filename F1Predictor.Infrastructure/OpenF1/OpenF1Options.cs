namespace F1Predictor.Infrastructure.OpenF1;

/// <summary>
/// Settings for the OpenF1 client, bound from the "OpenF1" configuration section.
/// </summary>
internal sealed class OpenF1Options
{
    public const string SectionName = "OpenF1";

    public string BaseAddress { get; set; } = "https://api.openf1.org/v1/";

    /// <summary>
    /// Minimum gap between outgoing requests. OpenF1 is free and community-run, so a
    /// full-season ingest should trickle rather than flood.
    /// </summary>
    public int RequestDelayMilliseconds { get; set; } = 500;

    public int TimeoutSeconds { get; set; } = 30;
}
