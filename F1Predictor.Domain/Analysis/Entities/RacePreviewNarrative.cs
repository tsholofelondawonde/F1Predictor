namespace F1Predictor.Domain.Analysis.Entities;

/// <summary>
/// A generated race preview, stored so a deployment without an AI provider can still serve
/// the last one written locally. One row per race session; regenerating replaces it.
/// </summary>
public class RacePreviewNarrative
{
    /// <summary>Column length of <see cref="Model"/>; the writer truncates to it and the EF configuration maps it.</summary>
    public const int ModelMaxLength = 100;

    /// <summary>Column length of <see cref="Headline"/>; the writer truncates to it and the EF configuration maps it.</summary>
    public const int HeadlineMaxLength = 300;

    public int Id { get; set; }

    /// <summary>The race session previewed. Unique.</summary>
    public int SessionKey { get; set; }

    /// <summary>Whether the real grid was known when this was written. False means the grid was projected.</summary>
    public bool GridConfirmed { get; set; }

    /// <summary>
    /// The latest classified session at generation time. A newer result means the standings and
    /// form this was written from have moved on, which is what makes it stale.
    /// </summary>
    public int? BasedOnLatestClassifiedSessionKey { get; set; }

    /// <summary>Chat model that wrote it, e.g. "llama3.1:8b".</summary>
    public string Model { get; set; } = "";

    public DateTimeOffset GeneratedAt { get; set; }

    /// <summary>First heading of the content, for cards and titles.</summary>
    public string Headline { get; set; } = "";

    /// <summary>Full markdown body.</summary>
    public string Content { get; set; } = "";
}
