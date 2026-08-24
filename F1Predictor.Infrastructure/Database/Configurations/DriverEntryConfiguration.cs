using F1Predictor.Domain.RaceData.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace F1Predictor.Infrastructure.Database.Configurations;

internal sealed class DriverEntryConfiguration : IEntityTypeConfiguration<DriverEntry>
{
    public void Configure(EntityTypeBuilder<DriverEntry> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.FullName).HasMaxLength(200).IsRequired();
        builder.Property(d => d.NameAcronym).HasMaxLength(10).IsRequired();
        builder.Property(d => d.TeamName).HasMaxLength(100).IsRequired();
        builder.Property(d => d.TeamColour).HasMaxLength(10).IsRequired();

        // Unique because OpenF1 lists a driver once per session; a duplicate means an
        // ingest wrote the same session twice, which the force path must not do.
        builder.HasIndex(d => new { d.SessionKey, d.DriverNumber }).IsUnique();
    }
}
