using F1Predictor.Application.Abstractions.Messaging;

namespace F1Predictor.Application.Features.Seasons.Ingest;

/// <summary>
/// Pulls a whole season of race weekends from OpenF1 into the raw tables, including sprints
/// and races that have not run yet.
/// Safe to re-run: sessions already stored with results are skipped rather than duplicated.
/// </summary>
public sealed class IngestSeasonCommand : ICommand<IngestSeasonResponse>
{
    public int Year { get; set; }

    /// <summary>
    /// Re-fetches and replaces sessions that are already stored, instead of skipping them.
    /// </summary>
    /// <remarks>
    /// Needed whenever a session's stored shape has to change — a new column to backfill, or
    /// a provisional classification that OpenF1 has since revised. Without it a session is
    /// written exactly once and never revisited.
    /// </remarks>
    public bool Force { get; set; }
}
