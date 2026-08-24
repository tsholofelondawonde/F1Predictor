using F1Predictor.Application.Abstractions.Messaging;

namespace F1Predictor.Application.Features.Predictions.GetHoldout;

/// <summary>
/// Predictions for the race that was held out of training, alongside what actually happened.
/// This is the "did the model learn anything real" check.
/// </summary>
public sealed record GetHoldoutPredictionsQuery(int Year) : IQuery<HoldoutPredictionsResponse>;
