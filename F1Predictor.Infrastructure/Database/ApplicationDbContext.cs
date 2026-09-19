using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Domain.Analysis.Entities;
using F1Predictor.Domain.Predictions;
using F1Predictor.Domain.RaceData.Entities;
using F1Predictor.Infrastructure.Database.Entities;
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
    public DbSet<DriverEntry> DriverEntries { get; set; } = null!;
    public DbSet<DriverRaceFeature> DriverRaceFeatures { get; set; } = null!;
    public DbSet<RacePreviewNarrative> RacePreviewNarratives { get; set; } = null!;
    internal DbSet<RaceEmbeddingRow> RaceEmbeddingRows { get; set; } = null!;

    public Task AcquireSessionAdvisoryLockAsync(int sessionKey, CancellationToken cancellationToken) =>
        Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({sessionKey})", cancellationToken);

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        int result = await base.SaveChangesAsync(cancellationToken);

        await PublishDomainEventsAsync();

        return result;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Required for RaceEmbeddingRow.Embedding (Pgvector's `vector` column type) to exist at all.
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        if (!string.Equals(Database.ProviderName, "Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal))
        {
            // Pgvector's `vector` column type (RaceEmbeddingRow.Embedding) is only ever mapped on
            // the Npgsql provider (UseVector(), wired in DependencyInjection.cs and
            // ApplicationDbContextFactory.cs). The Application test suite builds this context
            // against Microsoft.EntityFrameworkCore.InMemory, which has no mapping for the Vector
            // CLR type at all and fails model validation on it -- exclude the table from the model
            // entirely on any non-Npgsql provider rather than fight for a mapping that provider can
            // never have. No test exercises IRaceEmbeddingIndex against InMemoryDb; that port is
            // implemented and tested against the real Npgsql provider only.
            modelBuilder.Ignore<RaceEmbeddingRow>();
        }

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

