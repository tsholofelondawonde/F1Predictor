using F1Predictor.Domain.Predictions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace F1Predictor.Infrastructure.Database.Configurations;

internal sealed class ModelTrainingRunConfiguration : IEntityTypeConfiguration<ModelTrainingRun>
{
    public void Configure(EntityTypeBuilder<ModelTrainingRun> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Target).HasMaxLength(ModelTrainingRun.TargetMaxLength);
        builder.Property(r => r.TrainerName).HasMaxLength(ModelTrainingRun.TrainerNameMaxLength);
        builder.Property(r => r.FeatureNames).HasMaxLength(ModelTrainingRun.FeatureNamesMaxLength);
        builder.Property(r => r.Notes).HasMaxLength(ModelTrainingRun.NotesMaxLength);

        // The runs list reads newest first, optionally filtered to one target.
        builder.HasIndex(r => new { r.Target, r.TrainedAt });
    }
}
