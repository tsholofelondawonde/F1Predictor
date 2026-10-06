namespace F1Predictor.Application.Abstractions.AI;

/// <summary>
/// What the configured AI provider can do. The counterpart of
/// <see cref="MachineLearning.IRacePredictor.ModelsAvailable"/>: handlers check this before
/// touching <c>IChatClient</c>, which throws when no provider is configured.
/// </summary>
public interface IAiCapabilities
{
    bool ChatAvailable { get; }

    /// <summary>Reserved for stage 4; always false until an embedding provider exists.</summary>
    bool EmbeddingsAvailable { get; }

    /// <summary>"None", "Ollama" or "OpenAi".</summary>
    string Provider { get; }

    string? Model { get; }

    string? EmbeddingModel { get; }
}
