using F1Predictor.Domain.RaceData.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace F1Predictor.Infrastructure.Database.Configurations;

internal sealed class MeetingConfiguration : IEntityTypeConfiguration<Meeting>
{
    public void Configure(EntityTypeBuilder<Meeting> builder)
    {
        builder.HasKey(m => m.MeetingKey);

        // OpenF1 assigns the key; the database must never invent one.
        builder.Property(m => m.MeetingKey).ValueGeneratedNever();

        builder.Property(m => m.CircuitShortName).HasMaxLength(100).IsRequired();
        builder.Property(m => m.CountryName).HasMaxLength(100).IsRequired();
        builder.Property(m => m.MeetingName).HasMaxLength(200).IsRequired();

        builder.HasIndex(m => m.Year);
    }
}
