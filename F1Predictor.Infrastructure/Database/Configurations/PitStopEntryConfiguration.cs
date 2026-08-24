using F1Predictor.Domain.RaceData.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace F1Predictor.Infrastructure.Database.Configurations;

internal sealed class PitStopEntryConfiguration : IEntityTypeConfiguration<PitStopEntry>
{
    public void Configure(EntityTypeBuilder<PitStopEntry> builder)
    {
        builder.HasKey(p => p.Id);

        // Derived from StopDuration/LaneDuration in the domain; nothing to persist.
        builder.Ignore(p => p.EffectiveDuration);

        builder.HasIndex(p => new { p.SessionKey, p.DriverNumber });
    }
}
