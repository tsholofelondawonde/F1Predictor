using F1Predictor.Application.Abstractions.Messaging;

namespace F1Predictor.Application.Features.Analysis.FindSimilarRaces;

public sealed record FindSimilarRacesQuery(int SessionKey, int Top = 5) : IQuery<SimilarRacesResponse>;
