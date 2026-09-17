using F1Predictor.Domain.Analysis.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace F1Predictor.Infrastructure.Database.Configurations;

internal sealed class RacePreviewNarrativeConfiguration : IEntityTypeConfiguration<RacePreviewNarrative>
{
    public void Configure(EntityTypeBuilder<RacePreviewNarrative> builder)
    {
        builder.HasKey(n => n.Id);
        builder.HasIndex(n => n.SessionKey).IsUnique();
        builder.Property(n => n.Model).HasMaxLength(100);
        builder.Property(n => n.Headline).HasMaxLength(300);
        // Content is unbounded markdown; Npgsql maps string to text by default.
    }
}
