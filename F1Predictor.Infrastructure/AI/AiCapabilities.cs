using F1Predictor.Application.Abstractions.AI;
using Microsoft.Extensions.Options;

namespace F1Predictor.Infrastructure.AI;

internal sealed class AiCapabilities(IOptions<AiOptions> options) : IAiCapabilities
{
    private readonly AiOptions _options = options.Value;

    public bool ChatAvailable => _options.Provider is AiProvider.Ollama or AiProvider.OpenAi;

    public bool EmbeddingsAvailable => false;

    public string Provider => _options.Provider.ToString();

    public string? Model => _options.Provider switch
    {
        AiProvider.Ollama => _options.Ollama.Model,
        AiProvider.OpenAi => _options.OpenAi.Model,
        _ => null
    };

    public string? EmbeddingModel => null;
}
