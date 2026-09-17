using F1Predictor.Application.Abstractions.AI;
using Microsoft.Extensions.Options;

namespace F1Predictor.Infrastructure.AI;

internal sealed class AiCapabilities(IOptions<AiOptions> options) : IAiCapabilities
{
    private readonly AiOptions _options = options.Value;

    public bool ChatAvailable => _options.Provider == AiProvider.Ollama;

    public bool EmbeddingsAvailable => false;

    public string Provider => _options.Provider.ToString();

    public string? Model => ChatAvailable ? _options.Ollama.Model : null;

    public string? EmbeddingModel => null;
}
