using F1Predictor.Application.Abstractions.Messaging;

namespace F1Predictor.Application.Features.Championship.GetStandings;

/// <summary>
/// Both championship tables as they stand, sprint points included.
/// </summary>
public sealed record GetStandingsQuery(int Year) : IQuery<StandingsResponse>;
