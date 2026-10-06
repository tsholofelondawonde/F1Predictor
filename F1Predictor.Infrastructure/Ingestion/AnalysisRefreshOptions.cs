namespace F1Predictor.Infrastructure.Ingestion;

/// <summary>
/// Settings for the <see cref="AnalysisRefreshJob"/> interval backstop, bound from the
/// "AnalysisRefresh" configuration section. The on-demand trigger fired by
/// <see cref="SeasonIngestionCoordinatorJob"/> after a successful ingest is unconditional and
/// does not read this section.
/// </summary>
internal sealed class AnalysisRefreshOptions
{
    public const string SectionName = "AnalysisRefresh";

    /// <summary>
    /// Whether the interval-backstop trigger is registered at all. Independently gated on at
    /// least one AI channel being available — see <c>DependencyInjection.AddScheduler</c>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// How often the backstop re-checks every season for a stale or missing preview. Generous by
    /// design: the on-demand trigger from ingestion handles the common case, so this only needs to
    /// catch what that missed (a previously-unavailable AI provider coming back, a manual data fix).
    /// </summary>
    public int IntervalMinutes { get; set; } = 360;
}
