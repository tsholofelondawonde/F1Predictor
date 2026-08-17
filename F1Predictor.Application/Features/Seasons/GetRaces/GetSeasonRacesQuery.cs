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
/// <param name="DateStart">When the race started, or is scheduled to.</param>
/// <param name="IsSprint">
/// Whether this is the weekend's sprint rather than its Grand Prix. Sprints score championship
/// points but are never predicted — see <c>RaceSession.IsSprint</c>.
/// </param>
/// <param name="IsClassified">Whether results have been published. False means it has not run yet.</param>
/// <param name="FeatureRowCount">Driver rows available for this race; 0 means it cannot be predicted.</param>
public sealed record SeasonRaceResponse(
    int SessionKey,
    string MeetingName,
    string CircuitShortName,
    string CountryName,
    DateTimeOffset DateStart,
    bool IsSprint,
    bool IsClassified,
    int FeatureRowCount);
