using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Analysis.RefreshAnalysis;
using Microsoft.Extensions.Logging;
using Quartz;

namespace F1Predictor.Infrastructure.Ingestion;

/// <summary>
/// Ticks on a Quartz schedule (or is triggered on demand by <see cref="SeasonIngestionCoordinatorJob"/>)
/// and bridges straight to <see cref="RefreshAnalysisCommand"/> — no business logic lives here, just
/// the Quartz-to-handler bridge and a log line. The staleness rules live in the command's handler,
/// in the Application layer.
/// </summary>
[DisallowConcurrentExecution]
internal sealed class AnalysisRefreshJob(
    ICommandHandler<RefreshAnalysisCommand, RefreshAnalysisResponse> handler,
    ILogger<AnalysisRefreshJob> logger)
    : IJob
{
    public static readonly JobKey Key = new(nameof(AnalysisRefreshJob));

    public async Task Execute(IJobExecutionContext context)
    {
        int? year = context.JobDetail.JobDataMap.TryGetValue("year", out var yearValue) && yearValue is int y
            ? y
            : null;

        var result = await handler.Handle(new RefreshAnalysisCommand { Year = year }, context.CancellationToken);

        if (result.IsSuccess)
        {
            logger.LogInformation(
                "Analysis refresh: {Generated} preview(s) generated, {Indexed} race(s) indexed, {Skipped} skipped. {Notes}",
                result.Value.PreviewsGenerated, result.Value.RacesIndexed, result.Value.Skipped, string.Join(' ', result.Value.Notes));
        }
        else
        {
            logger.LogWarning("Analysis refresh failed: {Error}", result.Error.Code);
        }
    }
}
