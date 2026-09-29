using F1Predictor.Application.Abstractions.MachineLearning;
using F1Predictor.Domain.Predictions;

namespace F1Predictor.Application.Features.Predictions.Evaluation;

/// <summary>
/// Splits feature rows into train and validation by <em>race</em>, in date order: the latest
/// races validate, everything before them trains.
/// </summary>
/// <remarks>
/// This replaces a random 80/20 split over rows, which leaked in two ways: rows from one race
/// landed on both sides (so the model was "validated" on a race it had partly seen), and
/// later races trained a model scored on earlier ones. A prediction is always about a race
/// that has not happened yet, so validation has to look like that too.
/// </remarks>
internal static class RaceTimeSplit
{
    /// <summary>Share of races (not rows) held back for validation.</summary>
    public const double ValidationFraction = 0.2;

    /// <summary>
    /// A single race is ~20 rows with three podium finishers — too few for AUC to mean much —
    /// so validation never drops below two.
    /// </summary>
    public const int MinimumValidationRaces = 2;

    public const int MinimumTrainingRaces = 2;

    /// <summary>Fewest races (holdout excluded) that can be split at all.</summary>
    public const int MinimumRaces = MinimumValidationRaces + MinimumTrainingRaces;

    /// <param name="rows">Feature rows. Rows whose race is not in <paramref name="races"/> (the holdout) are dropped.</param>
    /// <param name="races">The races to split, in any order, holdout already excluded.</param>
    public static RaceSplit Split(IReadOnlyList<DriverRaceFeature> rows, IReadOnlyList<SeasonRace> races)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(races);

        if (races.Count < MinimumRaces)
        {
            throw new ArgumentException(
                $"Need at least {MinimumRaces} races to split; got {races.Count}.", nameof(races));
        }

        // Session keys are not chronological across seasons, so order on the date, with the key
        // only as a deterministic tie-break.
        var chronological = races.OrderBy(r => r.DateStart).ThenBy(r => r.SessionKey).ToList();

        var validationCount = Math.Max(
            MinimumValidationRaces,
            (int)Math.Round(chronological.Count * ValidationFraction, MidpointRounding.AwayFromZero));
        validationCount = Math.Min(validationCount, chronological.Count - MinimumTrainingRaces);

        var trainingRaces = chronological.Take(chronological.Count - validationCount).ToList();
        var validationRaces = chronological.Skip(chronological.Count - validationCount).ToList();

        var trainKeys = trainingRaces.Select(r => r.SessionKey).ToHashSet();
        var validationKeys = validationRaces.Select(r => r.SessionKey).ToHashSet();

        var data = new TrainingData(
            [.. rows.Where(f => trainKeys.Contains(f.SessionKey))],
            [.. rows.Where(f => validationKeys.Contains(f.SessionKey))]);

        return new RaceSplit(data, trainingRaces, validationRaces);
    }
}

/// <param name="Data">The rows, split.</param>
/// <param name="TrainingRaces">Races on the train side, oldest first.</param>
/// <param name="ValidationRaces">Races on the validation side, oldest first.</param>
internal sealed record RaceSplit(
    TrainingData Data,
    IReadOnlyList<SeasonRace> TrainingRaces,
    IReadOnlyList<SeasonRace> ValidationRaces);
