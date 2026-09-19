namespace F1Predictor.Application.Abstractions.AI;

/// <summary>
/// Vector search over race fact sheets (<c>F1Predictor.Domain.Analysis.RaceFactSheet</c>). This
/// is a port like <c>IOpenF1Client</c>, not a repository behind <c>IApplicationDbContext</c>:
/// nearest-neighbour search needs pgvector operators (<c>&lt;=&gt;</c> and friends) that no
/// generic LINQ query over <c>IApplicationDbContext</c> can express, so the implementation is
/// free to reach for a concrete, Postgres-specific <c>DbContext</c> instead.
/// </summary>
public interface IRaceEmbeddingIndex
{
    /// <summary>The stored embedding record for one session, or null if it has never been indexed.</summary>
    Task<RaceEmbeddingRecord?> GetAsync(int sessionKey, CancellationToken ct);

    /// <summary>Inserts or replaces the embedding row for <paramref name="record"/>'s session key.
    /// <paramref name="vector"/> is the embedding itself, kept separate from the record because
    /// the record is what a caller reads back, while the vector only ever needs to be written.</summary>
    Task UpsertAsync(RaceEmbeddingRecord record, ReadOnlyMemory<float> vector, CancellationToken ct);

    /// <summary>Nearest neighbours of an arbitrary query vector (e.g. an embedded question), by
    /// cosine similarity, restricted to rows embedded by the currently configured model.
    /// <paramref name="excludeSessionKey"/> lets a caller exclude a race from its own search
    /// results.</summary>
    Task<IReadOnlyList<RaceEmbeddingMatch>> SearchAsync(ReadOnlyMemory<float> query, int top, int? excludeSessionKey, CancellationToken ct);

    /// <summary>
    /// Nearest neighbours of the race already stored at <paramref name="sessionKey"/> — "races
    /// like this one". Unlike <see cref="SearchAsync"/>, this makes no embedding call: it reuses
    /// the vector already stored for <paramref name="sessionKey"/>, so it is cheap and, unlike
    /// every other AI-layer operation in this codebase, works even if the embedding provider is
    /// currently down or misconfigured — the comparison never leaves the database.
    /// </summary>
    Task<IReadOnlyList<RaceEmbeddingMatch>> SearchLikeAsync(int sessionKey, int top, CancellationToken ct);
}

/// <summary>One race's stored embedding metadata (never the vector itself — callers that need
/// the vector go through <see cref="IRaceEmbeddingIndex.SearchAsync"/> or
/// <see cref="IRaceEmbeddingIndex.SearchLikeAsync"/> instead of reading it back directly).</summary>
/// <param name="SessionKey">The embedded race.</param>
/// <param name="Model">The embedding model this vector was produced by, e.g. "text-embedding-3-small" —
/// matched against the currently configured model before a row is treated as a search candidate,
/// so a model change can coexist with the old model's rows while re-embedding is in progress.</param>
/// <param name="Dimensions">The vector's dimensionality, for a cheap sanity check before use.</param>
/// <param name="ContentHash">The fact sheet's <c>RaceFactSheet.ContentHash</c> at embedding time —
/// compared against a freshly built fact sheet's hash to decide whether a re-embed is needed.</param>
/// <param name="Text">The fact sheet text that was embedded, returned alongside the metadata so a
/// caller doesn't need a second round trip just to show what was indexed.</param>
/// <param name="EmbeddedAt">When this row was last written.</param>
public sealed record RaceEmbeddingRecord(int SessionKey, string Model, int Dimensions, string ContentHash, string Text, DateTimeOffset EmbeddedAt);

/// <summary>One race returned by a similarity search.</summary>
/// <param name="SessionKey">The matched race.</param>
/// <param name="Similarity">Cosine similarity in <c>[-1, 1]</c> (in practice <c>[0, 1]</c> for
/// normalised embeddings) — <c>1 - cosine distance</c>, computed server-side so higher always
/// means more similar.</param>
/// <param name="Text">The matched race's fact sheet text.</param>
public sealed record RaceEmbeddingMatch(int SessionKey, double Similarity, string Text);
