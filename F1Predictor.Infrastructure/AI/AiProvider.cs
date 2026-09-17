namespace F1Predictor.Infrastructure.AI;

internal enum AiProvider
{
    None,
    Ollama,
    /// <summary>Accepted by configuration, implemented in stage 4. Fails fast at startup until then.</summary>
    OpenAi
}
