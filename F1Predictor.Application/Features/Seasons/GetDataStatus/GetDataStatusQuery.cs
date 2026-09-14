using F1Predictor.Application.Abstractions.Messaging;

namespace F1Predictor.Application.Features.Seasons.GetDataStatus;

/// <summary>
/// Whether a season's standings and predictions are current with what has actually raced.
/// </summary>
public sealed record GetDataStatusQuery(int Year) : IQuery<DataStatusResponse>;
