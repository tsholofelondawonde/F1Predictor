namespace F1Predictor.Application.Features.Analysis.GenerateRacePreview;

/// <param name="SessionKey">The previewed race session's key.</param>
/// <param name="MeetingName">The previewed race weekend's meeting name.</param>
/// <param name="GridConfirmed">
/// True when the preview was written from the real starting grid; false when it was written
/// from a grid projected from recent form.
/// </param>
/// <param name="Model">The chat model that wrote the preview.</param>
/// <param name="GeneratedAt">When the preview was generated.</param>
/// <param name="Headline">The preview's headline, taken from its first markdown heading.</param>
/// <param name="Content">The full markdown body.</param>
/// <param name="Stale">
/// True when the preview was generated from a projected grid but the real grid now exists, or
/// a race has been classified since — either way, the numbers it was written from have moved on.
/// </param>
public sealed record RacePreviewResponse(
    int SessionKey,
    string MeetingName,
    bool GridConfirmed,
    string Model,
    DateTimeOffset GeneratedAt,
    string Headline,
    string Content,
    bool Stale);
