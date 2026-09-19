using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Seasons.Ingest;
using SharedKernel;

namespace F1Predictor.Infrastructure.Tests.Ingestion;

/// <summary>
/// Records every year it was asked to (re-)ingest, without touching OpenF1 or the database, so
/// the coordinator's due-rule can be exercised without a real ingest running behind it.
/// </summary>
internal sealed class FakeIngestSeasonCommandHandler : ICommandHandler<IngestSeasonCommand, IngestSeasonResponse>
{
    public List<int> HandledYears { get; } = [];

    /// <summary>
    /// Overrides the <see cref="IngestSeasonResponse.ClassifiedSessionKeys"/> / <see cref="IngestSeasonResponse.GridStored"/>
    /// returned for every handled year, so a test can exercise the coordinator's on-demand
    /// analysis-refresh trigger without a real ingest running behind it.
    /// </summary>
    public (IReadOnlyList<int> ClassifiedSessionKeys, bool GridStored) Signal { get; set; } = ([], false);

    public Task<Result<IngestSeasonResponse>> Handle(IngestSeasonCommand command, CancellationToken cancellationToken)
    {
        HandledYears.Add(command.Year);

        return Task.FromResult(Result.Success(new IngestSeasonResponse(
            command.Year, 0, 0, [], Signal.ClassifiedSessionKeys, Signal.GridStored)));
    }
}
