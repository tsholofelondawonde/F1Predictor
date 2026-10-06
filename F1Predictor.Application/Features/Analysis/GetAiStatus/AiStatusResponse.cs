namespace F1Predictor.Application.Features.Analysis.GetAiStatus;

/// <param name="ChatAvailable">Whether narrative generation and the analyst chat are on.</param>
/// <param name="EmbeddingsAvailable">Reserved for semantic search; always false for now.</param>
/// <param name="Provider">"None", "Ollama" or "OpenAi".</param>
/// <param name="Model">Chat model name, or null when chat is unavailable.</param>
/// <param name="EmbeddingModel">Embedding model name, or null.</param>
public sealed record AiStatusResponse(
    bool ChatAvailable, bool EmbeddingsAvailable, string Provider, string? Model, string? EmbeddingModel);
