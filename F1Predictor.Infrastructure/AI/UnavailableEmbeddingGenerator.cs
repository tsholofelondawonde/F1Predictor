using Microsoft.Extensions.AI;

namespace F1Predictor.Infrastructure.AI;

/// <summary>Mirrors <see cref="UnavailableChatClient"/> for the embeddings channel.</summary>
internal sealed class UnavailableEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private const string Message = "No embedding provider is configured (Ai:Embeddings:Provider is None). Check IAiCapabilities.EmbeddingsAvailable before calling the embedding generator.";

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(Message);

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
