using Microsoft.Extensions.Configuration;

namespace F1Predictor.Infrastructure.AI;

/// <summary>Bound from the "Ai" configuration section. Defaults to no provider, which fails closed.</summary>
internal sealed class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>
    /// Master switch. <c>false</c> pauses the whole AI layer — chat and embeddings alike —
    /// whatever providers and keys user secrets carry, so they need not be deleted to turn it
    /// off. Applied by <see cref="ApplyMasterSwitch"/>, never read anywhere else.
    /// </summary>
    public bool Enabled { get; set; } = true;

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

    /// <summary>
    /// Folds <see cref="Enabled"/> into the two provider settings, so a disabled layer is
    /// indistinguishable from one with no provider configured and every existing
    /// <c>Provider</c> check downstream keeps working unchanged.
    /// </summary>
    public void ApplyMasterSwitch()
    {
        if (Enabled)
        {
            return;
        }

        Provider = AiProvider.None;
        Embeddings.Provider = EmbeddingsProvider.None;
    }

    /// <summary>Binds the section eagerly (for schedule/registration-time decisions) with the master switch applied.</summary>
    public static AiOptions Read(IConfiguration configuration)
    {
        var options = configuration.GetSection(SectionName).Get<AiOptions>() ?? new AiOptions();
        options.ApplyMasterSwitch();
        return options;
    }

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
