namespace F1Predictor.Application.Features.Seasons.GetDataStatus;

/// <param name="IsStale">True if any of the three signals below means the pipeline needs a re-run.</param>
/// <param name="PendingRaceName">
/// Name of the earliest Grand Prix that has happened but has no classified result yet, or null if
/// none. Null cannot distinguish "nothing pending" from "OpenF1 never published this round's
/// result" — the same ambiguity <c>ChampionshipPoints</c> already accepts elsewhere.
/// </param>
/// <param name="PendingRaceDate">Start date of <see cref="PendingRaceName"/>, if any.</param>
/// <param name="FeaturesStale">True if a classified Grand Prix has no feature rows yet.</param>
/// <param name="ModelsAvailable">False if the models have never been trained.</param>
public sealed record DataStatusResponse(
    bool IsStale,
    string? PendingRaceName,
    DateTimeOffset? PendingRaceDate,
    bool FeaturesStale,
    bool ModelsAvailable);
