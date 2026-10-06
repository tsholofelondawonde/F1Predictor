using F1Predictor.Application.Abstractions.AI;
using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Seasons.Ingest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using SharedKernel;

namespace F1Predictor.Infrastructure.Ingestion;

/// <summary>
/// Ticks on a Quartz schedule and re-ingests a season only when there is a reason to: a race or
/// sprint session whose expected finish time has passed and is still unclassified, or an empty
/// database that has never been ingested at all.
/// </summary>
/// <remarks>
/// The tick itself is a single local query — no OpenF1 call happens unless something is actually
/// due — so this can run far more often than a season would ever need re-fetching without
/// wasting the free, rate-limited API on seasons that haven't changed.
/// </remarks>
[DisallowConcurrentExecution]
internal sealed class SeasonIngestionCoordinatorJob(
    IApplicationDbContext dbContext,
    ICommandHandler<IngestSeasonCommand, IngestSeasonResponse> ingestHandler,
    IOptions<IngestionSchedulerOptions> options,
    IDateTimeProvider dateTimeProvider,
    IAiCapabilities ai,
    ILogger<SeasonIngestionCoordinatorJob> logger)
    : IJob
{
    private bool HasAnalysisWork => ai.ChatAvailable || ai.EmbeddingsAvailable;

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        var dueYears = await DueYearsAsync(cancellationToken);

        if (dueYears.Count == 0 && !await dbContext.RaceSessions.AnyAsync(cancellationToken))
        {
            // Nothing has ever been ingested — seed the database rather than waiting for a
            // session to become "due" first.
            dueYears.Add(dateTimeProvider.UtcNow.Year);
        }

        if (dueYears.Count == 0)
        {
            logger.LogDebug("Ingestion coordinator: nothing due.");
            return;
        }

        foreach (var year in dueYears)
        {
            var result = await ingestHandler.Handle(
                new IngestSeasonCommand { Year = year, Force = false }, cancellationToken);

            if (result.IsSuccess)
            {
                logger.LogInformation(
                    "Ingestion coordinator: {Year} refreshed ({MeetingsIngested}/{MeetingsFound} weekends ingested).",
                    year, result.Value.MeetingsIngested, result.Value.MeetingsFound);

                // Fire-and-forget: TriggerJob only enqueues the run on Quartz's own thread pool.
                // Never await AnalysisRefreshJob's handler inline here — a slow chat completion
                // (up to TimeoutSeconds, no retries) blocking this loop would delay every other
                // due season behind it, which is exactly the failure mode this split avoids.
                // Skipped outright while the AI layer is off (Ai:Enabled=false or no provider):
                // the refresh would only log that it had nothing to do.
                if (HasAnalysisWork && (result.Value.ClassifiedSessionKeys.Count > 0 || result.Value.GridStored))
                {
                    await context.Scheduler.TriggerJob(
                        AnalysisRefreshJob.Key,
                        new JobDataMap { ["year"] = year },
                        cancellationToken);
                }
            }
            else
            {
                logger.LogWarning(
                    "Ingestion coordinator: refreshing {Year} failed — {Error}.",
                    year, result.Error);
            }
        }
    }

    /// <summary>
    /// Years with a session due for re-ingestion, by either rule: a race or sprint whose
    /// expected finish time has passed and is still unclassified, or a race that has not run yet
    /// but whose qualifying (or Sprint Qualifying) session has — and whose grid has not been
    /// stored yet. The second rule exists because OpenF1 publishes the grid once qualifying
    /// finishes, well before the race itself is classified, and re-checking then is what turns a
    /// projected preview into a confirmed one without waiting for the race to finish.
    /// </summary>
    /// <remarks>Internal, not private, so the due-rule can be exercised directly in tests without standing up a Quartz <see cref="IJobExecutionContext"/>.</remarks>
    internal async Task<List<int>> DueYearsAsync(CancellationToken cancellationToken)
    {
        var now = new DateTimeOffset(dateTimeProvider.UtcNow, TimeSpan.Zero);
        var cutoff = now - TimeSpan.FromMinutes(options.Value.PostSessionBufferMinutes);

        var pastDueYears = await (
            from session in dbContext.RaceSessions
            join meeting in dbContext.Meetings on session.MeetingKey equals meeting.MeetingKey
            where !session.IsClassified && session.DateStart <= cutoff
            select meeting.Year)
            .Distinct()
            .ToListAsync(cancellationToken);

        var qualifyingDueYears = await (
            from session in dbContext.RaceSessions
            join meeting in dbContext.Meetings on session.MeetingKey equals meeting.MeetingKey
            where !session.IsClassified
                && session.DateStart > now
                && session.QualifyingDateStart != null
                && session.QualifyingDateStart <= cutoff
                && !dbContext.StartingGridEntries.Any(g => g.SessionKey == session.SessionKey)
            select meeting.Year)
            .Distinct()
            .ToListAsync(cancellationToken);

        return pastDueYears.Union(qualifyingDueYears).ToList();
    }
}
