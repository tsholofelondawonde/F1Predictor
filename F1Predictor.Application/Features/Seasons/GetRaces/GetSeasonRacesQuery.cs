using F1Predictor.Application.Abstractions.Messaging;

namespace F1Predictor.Application.Features.Seasons.GetRaces;

/// <summary>
/// The ingested races of a season, so a caller can pick one to predict.
/// </summary>
public sealed record GetSeasonRacesQuery(int Year) : IQuery<IReadOnlyList<SeasonRaceResponse>>;

/// <param name="SessionKey">OpenF1 session key, used to request predictions for this race.</param>
/// <param name="MeetingName">Race weekend name.</param>
/// <param name="CircuitShortName">Short circuit name.</param>
/// <param name="CountryName">Host country.</param>
/// <param name="DateStart">When the race started.</param>
/// <param name="FeatureRowCount">Driver rows available for this race; 0 means it cannot be predicted yet.</param>
public sealed record SeasonRaceResponse(
    int SessionKey,
    string MeetingName,
    string CircuitShortName,
    string CountryName,
    DateTimeOffset DateStart,
    int FeatureRowCount);
