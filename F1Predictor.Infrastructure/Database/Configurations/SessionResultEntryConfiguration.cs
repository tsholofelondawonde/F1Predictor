using F1Predictor.Domain.RaceData.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace F1Predictor.Infrastructure.Database.Configurations;

internal sealed class SessionResultEntryConfiguration : IEntityTypeConfiguration<SessionResultEntry>
{
    public void Configure(EntityTypeBuilder<SessionResultEntry> builder)
    {
        builder.HasKey(r => r.Id);

        builder.HasIndex(r => new { r.SessionKey, r.DriverNumber });
    }
}
