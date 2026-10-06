using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Analysis.ExplainDriverPrediction;

namespace F1Predictor.Application.Features.Analysis.ExplainRacePrediction;

/// <param name="SessionKey">The classified race's session key.</param>
/// <param name="DriverNumber">The car number to explain.</param>
/// <param name="IncludeNarrative">
/// Whether to layer an AI narrative over the contributions when a provider is available. The
/// endpoint keeps the default; the analyst's <c>explain_race_driver</c> tool passes false, because
/// it returns the contribution table only and a narrative there would be a second LLM generation
/// thrown away.
/// </param>
public sealed record ExplainRacePredictionQuery(int SessionKey, int DriverNumber, bool IncludeNarrative = true) : IQuery<DriverExplanationResponse>;
