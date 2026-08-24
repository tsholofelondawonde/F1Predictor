using F1Predictor.Domain.Predictions;
using F1Predictor.Domain.RaceData.Entities;
using Microsoft.EntityFrameworkCore;

namespace F1Predictor.Infrastructure.Legacy;

/// <summary>
/// Read-only view of the original console prototype's SQLite file.
/// </summary>
/// <remarks>
/// The entity types are shared with the live model, but the mapping is not: this context
/// declares only what the old file actually contains (default table names, keys, nothing
/// else) rather than picking up the current configurations, whose indexes and column limits
/// the legacy file has never had.
/// </remarks>
internal sealed class LegacySqliteContext(DbContextOptions<LegacySqliteContext> options) : DbContext(options)
{
    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<RaceSession> RaceSessions => Set<RaceSession>();
    public DbSet<StartingGridEntry> StartingGridEntries => Set<StartingGridEntry>();
    public DbSet<SessionResultEntry> SessionResultEntries => Set<SessionResultEntry>();
    public DbSet<PitStopEntry> PitStopEntries => Set<PitStopEntry>();
    public DbSet<WeatherReading> WeatherReadings => Set<WeatherReading>();
    public DbSet<DriverRaceFeature> DriverRaceFeatures => Set<DriverRaceFeature>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Meeting>().HasKey(m => m.MeetingKey);
        modelBuilder.Entity<RaceSession>().HasKey(s => s.SessionKey);
        modelBuilder.Entity<PitStopEntry>().Ignore(p => p.EffectiveDuration);

        // Championship columns, added long after the prototype was retired. Selecting them
        // would fail against a file whose tables have never had them; the importer sets
        // sensible values on the way in instead.
        modelBuilder.Entity<RaceSession>().Ignore(s => s.IsSprint);
        modelBuilder.Entity<RaceSession>().Ignore(s => s.IsClassified);
        modelBuilder.Entity<SessionResultEntry>().Ignore(r => r.Points);
    }
}
