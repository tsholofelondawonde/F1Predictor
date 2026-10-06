using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Analysis.FindSimilarRaces;

namespace F1Predictor.Application.Features.Analysis.SearchRaces;

/// <summary>
/// Unvalidated by design — queries carry no FluentValidation validator in this codebase.
/// <see cref="Text"/>'s length and <see cref="Top"/> are clamped/validated in
/// <c>F1Predictor.WebApi/Endpoints/Analysis/SearchRaces.cs</c> instead, the same place
/// <c>GetTitleScenarios</c> clamps its own <c>topN</c>.
/// </summary>
public sealed record SearchRacesQuery(string Text, int Top = 5) : IQuery<SimilarRacesResponse>;
