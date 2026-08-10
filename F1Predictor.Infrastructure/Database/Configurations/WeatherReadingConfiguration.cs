using F1Predictor.Domain.RaceData.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace F1Predictor.Infrastructure.Database.Configurations;

internal sealed class WeatherReadingConfiguration : IEntityTypeConfiguration<WeatherReading>
{
    public void Configure(EntityTypeBuilder<WeatherReading> builder)
    {
        builder.HasKey(w => w.Id);

        builder.HasIndex(w => w.SessionKey);
    }
}
