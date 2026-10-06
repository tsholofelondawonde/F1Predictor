namespace F1Predictor.Infrastructure.AI;

internal enum AiProvider
{
    None,
    Ollama,
    /// <summary>Hosted chat via the OpenAI SDK. Fails fast at startup without <c>Ai:OpenAi:ApiKey</c>.</summary>
    OpenAi
}
