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
    ILogger<SeasonIngestionCoordinatorJob> logger)
    : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;

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
