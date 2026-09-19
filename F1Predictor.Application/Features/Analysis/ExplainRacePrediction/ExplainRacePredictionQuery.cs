using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Analysis.ExplainDriverPrediction;

namespace F1Predictor.Application.Features.Analysis.ExplainRacePrediction;

/// <param name="SessionKey">The classified race's session key.</param>
/// <param name="DriverNumber">The car number to explain.</param>
public sealed record ExplainRacePredictionQuery(int SessionKey, int DriverNumber) : IQuery<DriverExplanationResponse>;
