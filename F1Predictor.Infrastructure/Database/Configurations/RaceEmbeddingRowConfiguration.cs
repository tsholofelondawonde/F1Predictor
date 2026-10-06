using F1Predictor.Infrastructure.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace F1Predictor.Infrastructure.Database.Configurations;

internal sealed class RaceEmbeddingRowConfiguration : IEntityTypeConfiguration<RaceEmbeddingRow>
{
    /// <summary>
    /// Must track <c>Ai:Embeddings:Dimensions</c> (see <c>AiOptions.EmbeddingsOptions</c>). EF's
    /// fluent API needs the column type as a compile-time string (<c>HasColumnType</c> doesn't
    /// take a runtime value), so this is hard-coded rather than read from configuration — a
    /// dimension change needs both a config edit and a new migration, which is the correct
    /// order anyway since it changes the schema.
    /// </summary>
    private const int EmbeddingDimensions = 1536;

    public void Configure(EntityTypeBuilder<RaceEmbeddingRow> builder)
    {
        builder.HasKey(r => r.SessionKey);

        // SessionKey is the race session's own OpenF1 key (an external, natural key -- the same
        // value RaceSession.SessionKey carries), never a surrogate identity: without this, EF's
        // Npgsql provider defaults an int PK to an identity column, matching the convention set by
        // RaceSessionConfiguration for the same reason.
        builder.Property(r => r.SessionKey).ValueGeneratedNever();

        builder.Property(r => r.Embedding).HasColumnType($"vector({EmbeddingDimensions})");

        // HNSW over cosine distance is what SearchAsync/SearchLikeAsync order by
        // (Embedding.CosineDistance(...)), so the operator class has to match.
        builder.HasIndex(r => r.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");

        // A future model change can coexist with the old model's rows while re-embedding is in
        // progress -- SearchAsync/SearchLikeAsync both filter on Model, so this index backs that filter.
        builder.HasIndex(r => r.Model);
    }
}
