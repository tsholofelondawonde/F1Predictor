using Microsoft.Extensions.AI;

namespace F1Predictor.Application.Tests.Fakes;

/// <summary>
/// Deterministic in-memory <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/> for tests: each
/// call returns a fixed-length vector derived from the input text's hash code, so identical text
/// always produces an identical vector and different text produces a different one — enough for
/// cosine-similarity assertions to behave meaningfully without a real provider.
/// </summary>
/// <remarks>
/// <see cref="ThrowIfCalled"/> is what lets a hash-short-circuit test prove the generator was
/// never invoked, the same spirit as <c>FakeChatClient.Throws</c>.
/// </remarks>
internal sealed class FakeEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private const int Dimensions = 8;

    public bool ThrowIfCalled { get; set; }

    public int CallCount { get; private set; }

    public List<string> Calls { get; } = [];

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (ThrowIfCalled)
        {
            throw new InvalidOperationException("FakeEmbeddingGenerator was not expected to be called.");
        }

        CallCount++;

        var embeddings = new GeneratedEmbeddings<Embedding<float>>();
        foreach (var value in values)
        {
            Calls.Add(value);
            embeddings.Add(new Embedding<float>(VectorFor(value)));
        }

        return Task.FromResult(embeddings);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }

    /// <summary>A small, deterministic, text-dependent vector — not a real embedding, but its
    /// components are always non-negative, so cosine similarity between any two of them stays
    /// in <c>(0, 1]</c> rather than risking a negative or zero result by chance.</summary>
    private static ReadOnlyMemory<float> VectorFor(string value)
    {
        var random = new Random(value.GetHashCode(StringComparison.Ordinal));
        var vector = new float[Dimensions];
        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] = (float)random.NextDouble() + 0.01f;
        }

        return vector;
    }
}
