using F1Predictor.Domain.Predictions;
using F1Predictor.Domain.RaceData.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace F1Predictor.Application.Abstractions.Data;

public interface IApplicationDbContext
{
    // Raw OpenF1 data, persisted verbatim: one row per API result, no transformation.
    public DbSet<Meeting> Meetings { get; set; }
    public DbSet<RaceSession> RaceSessions { get; set; }
    public DbSet<StartingGridEntry> StartingGridEntries { get; set; }
    public DbSet<SessionResultEntry> SessionResultEntries { get; set; }
    public DbSet<PitStopEntry> PitStopEntries { get; set; }
    public DbSet<WeatherReading> WeatherReadings { get; set; }
    public DbSet<DriverEntry> DriverEntries { get; set; }

    /// <summary>Model-ready rows derived from the raw tables above.</summary>
    public DbSet<DriverRaceFeature> DriverRaceFeatures { get; set; }

    /// <summary>
    /// Provides access to the change tracker for managing entity state.
    /// Used for advanced scenarios like clearing tracked entities during retry operations.
    /// </summary>
    ChangeTracker ChangeTracker { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
