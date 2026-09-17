using F1Predictor.Application.Abstractions.AI;

namespace F1Predictor.Application.Tests.Fakes;

internal sealed class FakeAiCapabilities(bool chatAvailable, string? model = "fake-model") : IAiCapabilities
{
    public bool ChatAvailable => chatAvailable;

    public bool EmbeddingsAvailable => false;

    public string Provider => chatAvailable ? "Fake" : "None";

    public string? Model => chatAvailable ? model : null;

    public string? EmbeddingModel => null;
}
