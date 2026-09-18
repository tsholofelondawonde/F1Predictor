using F1Predictor.Application.Abstractions.Messaging;

namespace F1Predictor.Application.Features.Analysis.ExplainDriverPrediction;

/// <param name="Year">Season the next Grand Prix belongs to.</param>
/// <param name="DriverNumber">The car number to explain.</param>
/// <param name="IncludeNarrative">
/// Whether to layer an AI narrative over the contributions when a provider is available. The
/// endpoint keeps the default; the analyst's <c>explain_driver</c> tool passes false, because it
/// returns the contribution table only and a narrative there would be a second LLM generation
/// thrown away.
/// </param>
public sealed record ExplainDriverPredictionQuery(int Year, int DriverNumber, bool IncludeNarrative = true) : IQuery<DriverExplanationResponse>;
