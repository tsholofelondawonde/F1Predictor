using F1Predictor.Application.Abstractions.AI;

namespace F1Predictor.Application.Tests.Fakes;

internal sealed class FakeAiCapabilities(
    bool chatAvailable, string? model = "fake-model", bool embeddingsAvailable = false, string? embeddingModel = "fake-embed-model")
    : IAiCapabilities
{
    public bool ChatAvailable => chatAvailable;

    public bool EmbeddingsAvailable => embeddingsAvailable;

    public string Provider => chatAvailable ? "Fake" : "None";

    public string? Model => chatAvailable ? model : null;

    public string? EmbeddingModel => embeddingsAvailable ? embeddingModel : null;
}
