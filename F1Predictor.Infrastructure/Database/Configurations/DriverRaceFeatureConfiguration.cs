using F1Predictor.Domain.Predictions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace F1Predictor.Infrastructure.Database.Configurations;

internal sealed class DriverRaceFeatureConfiguration : IEntityTypeConfiguration<DriverRaceFeature>
{
    public void Configure(EntityTypeBuilder<DriverRaceFeature> builder)
    {
        builder.HasKey(f => f.Id);

        // The table is cleared and regenerated wholesale, so one row per driver per race
        // is an invariant worth having the database enforce.
        builder.HasIndex(f => new { f.SessionKey, f.DriverNumber }).IsUnique();
    }
}
