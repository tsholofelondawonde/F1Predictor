using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Domain.Predictions;
using F1Predictor.Domain.RaceData.Entities;
using F1Predictor.Infrastructure.DomainEvents;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace F1Predictor.Infrastructure.Database;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IDomainEventsDispatcher? domainEventsDispatcher) : DbContext(options), IApplicationDbContext
{
    private readonly IDomainEventsDispatcher? _domainEventsDispatcher = domainEventsDispatcher;
    public DbSet<Meeting> Meetings { get; set; } = null!;
    public DbSet<RaceSession> RaceSessions { get; set; } = null!;
    public DbSet<StartingGridEntry> StartingGridEntries { get; set; } = null!;
    public DbSet<SessionResultEntry> SessionResultEntries { get; set; } = null!;
    public DbSet<PitStopEntry> PitStopEntries { get; set; } = null!;
    public DbSet<WeatherReading> WeatherReadings { get; set; } = null!;
    public DbSet<DriverRaceFeature> DriverRaceFeatures { get; set; } = null!;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        int result = await base.SaveChangesAsync(cancellationToken);

        await PublishDomainEventsAsync();

        return result;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.HasDefaultSchema(Schemas.GetDefaultSchema(Database.ProviderName));
    }

    private async Task PublishDomainEventsAsync()
    {
        if (_domainEventsDispatcher is null)
        {
            return;
        }

        var domainEvents = ChangeTracker
            .Entries<Entity>()
            .Select(entry => entry.Entity)
            .SelectMany(entity =>
            {
                List<IDomainEvent> domainEvents = entity.DomainEvents;

                entity.ClearDomainEvents();

                return domainEvents;
            })
            .ToList();

        await _domainEventsDispatcher.DispatchAsync(domainEvents);
    }
}

