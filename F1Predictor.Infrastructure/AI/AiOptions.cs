namespace F1Predictor.Infrastructure.AI;

/// <summary>Bound from the "Ai" configuration section. Defaults to no provider, which fails closed.</summary>
internal sealed class AiOptions
{
    public const string SectionName = "Ai";

    public AiProvider Provider { get; set; } = AiProvider.None;

    public OllamaOptions Ollama { get; set; } = new();

    public OpenAiOptions OpenAi { get; set; } = new();

    public EmbeddingsOptions Embeddings { get; set; } = new();

    /// <summary>Per-request HTTP timeout. No retries: a 60 s generation retried is worse than a failure.</summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>Upper bound on tool-call round trips per chat request.</summary>
    public int MaxToolIterations { get; set; } = 6;

    public float Temperature { get; set; } = 0.2f;

    /// <summary>Cost guard now that tokens cost money on the OpenAi path. gpt-5-mini is a
    /// reasoning-family model whose hidden reasoning tokens draw from this same budget, so too
    /// low a value starves the visible answer entirely (an empty completion, not an error) —
    /// verified against both the race-preview and analyst-chat routes at 800.</summary>
    public int MaxOutputTokens { get; set; } = 4096;

    internal sealed class OllamaOptions
    {
        public string Endpoint { get; set; } = "http://localhost:11434";

        /// <summary>Any Ollama model with tool support.</summary>
        public string Model { get; set; } = "llama3.1:8b";

        /// <summary>Ollama's default of 4096 is too small for a system prompt plus tool results.</summary>
        public int ContextLength { get; set; } = 16384;
    }

    internal sealed class OpenAiOptions
    {
        /// <summary>Null/empty uses the OpenAI SDK's own default (https://api.openai.com/v1).
        /// Set this to point at any OpenAI-compatible server instead, e.g. a local Docker
        /// Model Runner endpoint.</summary>
        public string? Endpoint { get; set; }

        /// <summary>Secret — user secrets locally, a Container Apps secret in prod. Never appsettings.
        /// An OpenAI-compatible local server typically ignores it, but the SDK still needs a
        /// non-null credential.</summary>
        public string ApiKey { get; set; } = string.Empty;

        public string ChatModel { get; set; } = "gpt-5-mini";
    }

    internal sealed class EmbeddingsOptions
    {
        /// <summary>Independent of <see cref="Provider"/> — dev can run Ollama chat + OpenAi embeddings.</summary>
        public EmbeddingsProvider Provider { get; set; } = EmbeddingsProvider.None;

        public string Model { get; set; } = "text-embedding-3-small";

        /// <summary>Must equal the pgvector column width once that lands. Not yet validated against
        /// a column — there is no vector column until the embeddings-index work ships.</summary>
        public int Dimensions { get; set; } = 1536;
    }
}
