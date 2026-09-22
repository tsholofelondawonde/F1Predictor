namespace F1Predictor.Application.Features.Analysis.FindSimilarRaces;

/// <param name="SourceSessionKey">
/// The race similarity was searched from, or 0 for a free-text search
/// (<c>F1Predictor.Application.Features.Analysis.SearchRaces.SearchRacesQuery</c>), which has no
/// source race.
/// </param>
/// <param name="SourceMeetingName">
/// The source race's meeting name, or the search text itself for a free-text search.
/// </param>
public sealed record SimilarRacesResponse(int SourceSessionKey, string SourceMeetingName, IReadOnlyList<SimilarRace> Races);

/// <param name="Similarity">Cosine similarity — <c>(0, 1]</c> in practice for normalised embeddings.</param>
/// <param name="Summary">The matched race's fact-sheet text.</param>
public sealed record SimilarRace(int SessionKey, int Year, string MeetingName, string CircuitShortName, DateTimeOffset DateStart, double Similarity, string Summary);
