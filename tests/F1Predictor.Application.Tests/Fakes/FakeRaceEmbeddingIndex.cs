using F1Predictor.Application.Abstractions.AI;

namespace F1Predictor.Application.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IRaceEmbeddingIndex"/> for tests. Cosine similarity is computed directly
/// over the stored vectors — the same maths the real pgvector-backed implementation
/// (<c>RaceEmbeddingIndex</c>) delegates to Postgres via the <c>&lt;=&gt;</c> operator.
/// </summary>
internal sealed class FakeRaceEmbeddingIndex : IRaceEmbeddingIndex
{
    private readonly Dictionary<int, (RaceEmbeddingRecord Record, float[] Vector)> _rows = [];

    public Task<RaceEmbeddingRecord?> GetAsync(int sessionKey, CancellationToken ct) =>
        Task.FromResult(_rows.TryGetValue(sessionKey, out var row) ? row.Record : null);

    public Task UpsertAsync(RaceEmbeddingRecord record, ReadOnlyMemory<float> vector, CancellationToken ct)
    {
        _rows[record.SessionKey] = (record, vector.ToArray());
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RaceEmbeddingMatch>> SearchAsync(ReadOnlyMemory<float> query, int top, int? excludeSessionKey, CancellationToken ct)
    {
        var queryVector = query.ToArray();

        IReadOnlyList<RaceEmbeddingMatch> matches = _rows.Values
            .Where(row => excludeSessionKey is null || row.Record.SessionKey != excludeSessionKey)
            .Select(row => new RaceEmbeddingMatch(row.Record.SessionKey, CosineSimilarity(queryVector, row.Vector), row.Record.Text))
            .OrderByDescending(m => m.Similarity)
            .Take(top)
            .ToList();

        return Task.FromResult(matches);
    }

    public Task<IReadOnlyList<RaceEmbeddingMatch>> SearchLikeAsync(int sessionKey, int top, CancellationToken ct)
    {
        if (!_rows.TryGetValue(sessionKey, out var source))
        {
            return Task.FromResult<IReadOnlyList<RaceEmbeddingMatch>>([]);
        }

        IReadOnlyList<RaceEmbeddingMatch> matches = _rows.Values
            .Where(row => row.Record.SessionKey != sessionKey)
            .Select(row => new RaceEmbeddingMatch(row.Record.SessionKey, CosineSimilarity(source.Vector, row.Vector), row.Record.Text))
            .OrderByDescending(m => m.Similarity)
            .Take(top)
            .ToList();

        return Task.FromResult(matches);
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        double dot = 0, magnitudeA = 0, magnitudeB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magnitudeA += a[i] * a[i];
            magnitudeB += b[i] * b[i];
        }

        return magnitudeA == 0 || magnitudeB == 0 ? 0 : dot / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
    }
}
