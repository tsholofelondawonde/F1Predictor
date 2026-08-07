using F1Predictor.Domain.RaceData.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace F1Predictor.Infrastructure.Database.Configurations;

internal sealed class StartingGridEntryConfiguration : IEntityTypeConfiguration<StartingGridEntry>
{
    public void Configure(EntityTypeBuilder<StartingGridEntry> builder)
    {
        builder.HasKey(g => g.Id);

        // Every read of this table filters by session, usually then by driver.
        builder.HasIndex(g => new { g.SessionKey, g.DriverNumber });
    }
}
