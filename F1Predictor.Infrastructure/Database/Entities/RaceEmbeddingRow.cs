using Pgvector;

namespace F1Predictor.Infrastructure.Database.Entities;

/// <summary>
/// EF entity backing <c>IRaceEmbeddingIndex</c> — kept in Infrastructure, not Domain, because its
/// <see cref="Embedding"/> property depends on the <c>Pgvector</c> package, and
/// <c>F1Predictor.Domain</c> stays free of any package beyond what it already references. One row
/// per race session; re-embedding replaces the row in place.
/// </summary>
internal sealed class RaceEmbeddingRow
{
    public int SessionKey { get; set; }

    /// <summary>The embedding model this vector was produced by, e.g. "text-embedding-3-small".</summary>
    public string Model { get; set; } = "";

    public int Dimensions { get; set; }

    /// <summary>The fact sheet's <c>RaceFactSheet.ContentHash</c> at embedding time.</summary>
    public string ContentHash { get; set; } = "";

    /// <summary>The fact sheet text that was embedded.</summary>
    public string Text { get; set; } = "";

    public DateTimeOffset EmbeddedAt { get; set; }

    public Vector Embedding { get; set; } = null!;
}
