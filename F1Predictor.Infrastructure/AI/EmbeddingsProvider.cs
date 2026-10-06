namespace F1Predictor.Infrastructure.AI;

/// <summary>Independent of <see cref="AiProvider"/> — chat can run on Ollama while embeddings run on OpenAi.</summary>
internal enum EmbeddingsProvider
{
    None,
    OpenAi
}
