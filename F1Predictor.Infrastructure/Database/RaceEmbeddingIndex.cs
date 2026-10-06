using F1Predictor.Application.Abstractions.AI;
using F1Predictor.Infrastructure.AI;
using F1Predictor.Infrastructure.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace F1Predictor.Infrastructure.Database;

/// <summary>
/// pgvector-backed implementation of <see cref="IRaceEmbeddingIndex"/>. Similarity is always
/// computed inside the LINQ <c>Select</c> (via <c>Vector.CosineDistance</c>, which
/// Pgvector.EntityFrameworkCore translates to the <c>&lt;=&gt;</c> operator), so ordering and the
/// similarity figure itself both run server-side rather than after materializing rows.
/// </summary>
internal sealed class RaceEmbeddingIndex(ApplicationDbContext context, IOptions<AiOptions> aiOptions) : IRaceEmbeddingIndex
{
    public async Task<RaceEmbeddingRecord?> GetAsync(int sessionKey, CancellationToken ct)
    {
        var row = await context.Set<RaceEmbeddingRow>().AsNoTracking()
            .FirstOrDefaultAsync(r => r.SessionKey == sessionKey, ct);

        return row is null
            ? null
            : new RaceEmbeddingRecord(row.SessionKey, row.Model, row.Dimensions, row.ContentHash, row.Text, row.EmbeddedAt);
    }

    public async Task UpsertAsync(RaceEmbeddingRecord record, ReadOnlyMemory<float> vector, CancellationToken ct)
    {
        var row = await context.Set<RaceEmbeddingRow>().FirstOrDefaultAsync(r => r.SessionKey == record.SessionKey, ct)
            ?? context.Set<RaceEmbeddingRow>().Add(new RaceEmbeddingRow { SessionKey = record.SessionKey }).Entity;

        row.Model = record.Model;
        row.Dimensions = record.Dimensions;
        row.ContentHash = record.ContentHash;
        row.Text = record.Text;
        row.EmbeddedAt = record.EmbeddedAt;
        row.Embedding = new Vector(vector);

        await context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<RaceEmbeddingMatch>> SearchAsync(ReadOnlyMemory<float> query, int top, int? excludeSessionKey, CancellationToken ct)
    {
        var model = aiOptions.Value.Embeddings.Model;
        var vector = new Vector(query);

        return await context.Set<RaceEmbeddingRow>().AsNoTracking()
            .Where(r => r.Model == model && (excludeSessionKey == null || r.SessionKey != excludeSessionKey))
            .OrderBy(r => r.Embedding.CosineDistance(vector))
            .Take(top)
            .Select(r => new RaceEmbeddingMatch(r.SessionKey, 1 - r.Embedding.CosineDistance(vector), r.Text))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<RaceEmbeddingMatch>> SearchLikeAsync(int sessionKey, int top, CancellationToken ct)
    {
        var source = await context.Set<RaceEmbeddingRow>().AsNoTracking()
            .FirstOrDefaultAsync(r => r.SessionKey == sessionKey, ct);

        if (source is null)
        {
            return [];
        }

        return await context.Set<RaceEmbeddingRow>().AsNoTracking()
            .Where(r => r.Model == source.Model && r.SessionKey != sessionKey)
            .OrderBy(r => r.Embedding.CosineDistance(source.Embedding))
            .Take(top)
            .Select(r => new RaceEmbeddingMatch(r.SessionKey, 1 - r.Embedding.CosineDistance(source.Embedding), r.Text))
            .ToListAsync(ct);
    }
}
