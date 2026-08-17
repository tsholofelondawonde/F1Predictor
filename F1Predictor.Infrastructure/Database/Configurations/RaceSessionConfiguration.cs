using F1Predictor.Domain.RaceData.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace F1Predictor.Infrastructure.Database.Configurations;

internal sealed class RaceSessionConfiguration : IEntityTypeConfiguration<RaceSession>
{
    public void Configure(EntityTypeBuilder<RaceSession> builder)
    {
        builder.HasKey(s => s.SessionKey);

        builder.Property(s => s.SessionKey).ValueGeneratedNever();

        builder.Property(s => s.SessionName).HasMaxLength(100).IsRequired();
        builder.Property(s => s.SessionType).HasMaxLength(100).IsRequired();

        builder.HasIndex(s => s.MeetingKey);
        builder.HasIndex(s => s.DateStart);

        // Every training and standings query filters on these two, and the "next race"
        // lookup is an ordered scan of the unclassified rows.
        builder.HasIndex(s => new { s.IsSprint, s.IsClassified });
    }
}
