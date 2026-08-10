using F1Predictor.Application.Abstractions.Messaging;

namespace F1Predictor.Application.Features.Seasons.Ingest;

/// <summary>
/// Pulls a whole season of race weekends from OpenF1 into the raw tables.
/// Safe to re-run: already-ingested race sessions are skipped rather than duplicated.
/// </summary>
public sealed class IngestSeasonCommand : ICommand<IngestSeasonResponse>
{
    public int Year { get; set; }
}
