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
        var cutoff = new DateTimeOffset(dateTimeProvider.UtcNow, TimeSpan.Zero)
            - TimeSpan.FromMinutes(options.Value.PostSessionBufferMinutes);

        var dueYears = await (
            from session in dbContext.RaceSessions
            join meeting in dbContext.Meetings on session.MeetingKey equals meeting.MeetingKey
            where !session.IsClassified && session.DateStart <= cutoff
            select meeting.Year)
            .Distinct()
            .ToListAsync(cancellationToken);

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
}
