using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Domain.Predictions;
using F1Predictor.Domain.RaceData.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace F1Predictor.Application.Features.Predictions.RebuildFeatures;

internal sealed class RebuildFeaturesCommandHandler(
    IApplicationDbContext context,
    ILogger<RebuildFeaturesCommandHandler> logger)
    : ICommandHandler<RebuildFeaturesCommand, RebuildFeaturesResponse>
{
    public async Task<Result<RebuildFeaturesResponse>> Handle(
        RebuildFeaturesCommand command,
        CancellationToken cancellationToken)
    {
        await context.DriverRaceFeatures.ExecuteDeleteAsync(cancellationToken);

        // The whole raw dataset is a few thousand rows, so pull it once and group in memory
        // rather than issuing four queries per race — the database is remote and round trips
        // dominate at this size.
        var sessions = await context.RaceSessions.AsNoTracking().ToListAsync(cancellationToken);
        var gridBySession = (await context.StartingGridEntries.AsNoTracking().ToListAsync(cancellationToken))
            .ToLookup(g => g.SessionKey);
        var resultsBySession = (await context.SessionResultEntries.AsNoTracking().ToListAsync(cancellationToken))
            .ToLookup(r => r.SessionKey);
        var pitsBySession = (await context.PitStopEntries.AsNoTracking().ToListAsync(cancellationToken))
            .ToLookup(p => p.SessionKey);
        var weatherBySession = (await context.WeatherReadings.AsNoTracking().ToListAsync(cancellationToken))
            .ToLookup(w => w.SessionKey);

        // Null means the race was skipped for missing grid or result data, as opposed to
        // producing an empty-but-valid set of rows.
        var perRace = sessions.ConvertAll(session => BuildRaceFeatures(
            gridBySession[session.SessionKey].ToList(),
            resultsBySession[session.SessionKey].ToList(),
            pitsBySession[session.SessionKey].ToLookup(p => p.DriverNumber),
            weatherBySession[session.SessionKey]));

        var racesSkipped = perRace.Count(rows => rows is null);
        var racesWithFeatures = perRace.Count - racesSkipped;
        var features = perRace.Where(rows => rows is not null).SelectMany(rows => rows!).ToList();

        context.DriverRaceFeatures.AddRange(features);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Rebuilt {FeatureRows} feature rows across {RaceCount} races ({SkippedCount} races skipped for missing data).",
            features.Count, racesWithFeatures, racesSkipped);

        return Result.Success(new RebuildFeaturesResponse(racesWithFeatures, racesSkipped, features.Count));
    }

    /// <summary>
    /// Builds every driver row for one race, or returns null if the race lacks the grid or
    /// result data the features are derived from.
    /// </summary>
    private static List<DriverRaceFeature>? BuildRaceFeatures(
        List<StartingGridEntry> grid,
        List<SessionResultEntry> results,
        ILookup<int, PitStopEntry> pitsByDriver,
        IEnumerable<WeatherReading> weather)
    {
        if (grid.Count == 0 || results.Count == 0)
        {
            return null;
        }

        var polePace = DriverRaceFeature.PolePaceOf(grid);
        var rainedInSession = DriverRaceFeature.RainedIn(weather);

        return results
            .Where(r => !r.Dns && r.Position.HasValue)
            .Select(result => DriverRaceFeature.Create(
                result,
                grid.Find(g => g.DriverNumber == result.DriverNumber),
                grid.Count,
                polePace,
                pitsByDriver[result.DriverNumber].ToList(),
                rainedInSession))
            .ToList();
    }
}
