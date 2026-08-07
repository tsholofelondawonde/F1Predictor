using Microsoft.EntityFrameworkCore;

namespace F1Predictor.Data;

public class AppDbContext : DbContext
{
    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<RaceSession> RaceSessions => Set<RaceSession>();
    public DbSet<StartingGridEntry> StartingGridEntries => Set<StartingGridEntry>();
    public DbSet<SessionResultEntry> SessionResultEntries => Set<SessionResultEntry>();
    public DbSet<PitStopEntry> PitStopEntries => Set<PitStopEntry>();
    public DbSet<WeatherReading> WeatherReadings => Set<WeatherReading>();
    public DbSet<DriverRaceFeature> DriverRaceFeatures => Set<DriverRaceFeature>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=f1predictor.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Meeting>().HasKey(m => m.MeetingKey);
        modelBuilder.Entity<RaceSession>().HasKey(s => s.SessionKey);
    }
}
