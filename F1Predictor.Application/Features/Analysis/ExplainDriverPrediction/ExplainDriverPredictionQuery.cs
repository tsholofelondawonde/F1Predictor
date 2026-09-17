using F1Predictor.Application.Abstractions.Messaging;

namespace F1Predictor.Application.Features.Analysis.ExplainDriverPrediction;

/// <param name="Year">Season the next Grand Prix belongs to.</param>
/// <param name="DriverNumber">The car number to explain.</param>
public sealed record ExplainDriverPredictionQuery(int Year, int DriverNumber) : IQuery<DriverExplanationResponse>;
