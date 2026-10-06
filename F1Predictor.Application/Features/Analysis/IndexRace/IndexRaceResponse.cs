namespace F1Predictor.Application.Features.Analysis.IndexRace;

/// <param name="SessionKey">The race session that was (or would have been) indexed.</param>
/// <param name="Reindexed">
/// True when a new embedding was written — the race had never been indexed, its fact sheet's
/// content changed, or the configured embedding model changed. False means the row already
/// stored was current, so the embedding generator was never called.
/// </param>
/// <param name="ContentHash">The fact sheet's content hash as of this call, whether or not a
/// re-embed happened.</param>
public sealed record IndexRaceResponse(int SessionKey, bool Reindexed, string ContentHash);
