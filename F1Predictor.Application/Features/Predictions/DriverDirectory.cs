using System.Globalization;
using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Abstractions.MachineLearning;
using F1Predictor.Application.Features.Predictions.GetHoldout;
using F1Predictor.Domain.Predictions;
using F1Predictor.Domain.RaceData.Entities;
using Microsoft.EntityFrameworkCore;

namespace F1Predictor.Application.Features.Predictions;

/// <summary>
/// Who was driving in a session, so a prediction can be reported against a name rather than a
/// bare car number.
/// </summary>
/// <remarks>
/// Races ingested before entry lists were captured have no rows here. Rather than fail, those
/// drivers fall back to a car-number placeholder — the prediction is still perfectly good, and
/// re-ingesting the season with <c>force=true</c> fills the names in.
/// </remarks>
internal sealed class DriverDirectory(Dictionary<int, DriverEntry> entries)
{
    public static async Task<DriverDirectory> ForSessionAsync(
        IApplicationDbContext context,
        int sessionKey,
        CancellationToken cancellationToken)
    {
        var entries = await context.DriverEntries
            .Where(d => d.SessionKey == sessionKey)
            .AsNoTracking()
            .ToDictionaryAsync(d => d.DriverNumber, cancellationToken);

        return new DriverDirectory(entries);
    }

    public DriverPrediction Describe(DriverRaceFeature feature, RaceProbabilities probabilities)
    {
        ArgumentNullException.ThrowIfNull(feature);
        ArgumentNullException.ThrowIfNull(probabilities);

        var entry = entries.GetValueOrDefault(feature.DriverNumber);
        var number = feature.DriverNumber.ToString(CultureInfo.InvariantCulture);

        return new DriverPrediction(
            feature.DriverNumber,
            entry?.FullName ?? $"Car {number}",
            entry?.NameAcronym ?? number,
            entry?.TeamName ?? "Unknown",
            entry?.TeamColour ?? "",
            feature.GridPosition,
            feature.FinishPosition,
            feature.Podium,
            feature.PointsFinish,
            probabilities.PodiumProbability,
            probabilities.PointsProbability);
    }
}
