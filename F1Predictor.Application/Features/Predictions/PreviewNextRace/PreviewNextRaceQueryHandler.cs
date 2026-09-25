using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Abstractions.MachineLearning;
using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Abstractions.OpenF1;
using F1Predictor.Domain.Predictions;
using F1Predictor.Domain.RaceData.Entities;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace F1Predictor.Application.Features.Predictions.PreviewNextRace;

/// <summary>
/// Previews the next Grand Prix, using the real starting grid where there is one and a projection
/// from recent form where there is not.
/// </summary>
/// <remarks>
/// The models take a grid position and a gap to pole, neither of which exists until qualifying has
/// run — which is most of the week. Rather than show nothing until Saturday, an unqualified race is
/// previewed against a grid projected from each driver's recent form, and the response says plainly
/// which of the two it is. The projection is an ordering of season averages, not the averages
/// themselves, so the model still sees a real permutation of grid slots.
/// </remarks>
internal sealed class PreviewNextRaceQueryHandler(
    IApplicationDbContext context,
    IRacePredictor predictor,
    IOpenF1Client openF1,
    HybridCache cache,
    ILogger<PreviewNextRaceQueryHandler> logger)
    : IQueryHandler<PreviewNextRaceQuery, NextRacePreviewResponse>
{
    public async Task<Result<NextRacePreviewResponse>> Handle(
        PreviewNextRaceQuery query,
        CancellationToken cancellationToken)
    {
        if (!predictor.ModelsAvailable)
        {
            return Result.Failure<NextRacePreviewResponse>(PredictionErrors.ModelsNotTrained);
        }

        var loaded = await NextRaceContext.LoadAsync(context, query.Year, cancellationToken);

        if (loaded.IsFailure)
        {
            return Result.Failure<NextRacePreviewResponse>(loaded.Error);
        }

        var race = loaded.Value.Race;
        var history = loaded.Value.History;
        var features = loaded.Value.Features;

        var drivers = loaded.Value.Entries
            .Select(entry => Describe(entry, features[entry.DriverNumber], history))
            .OrderByDescending(d => d.PodiumProbability)
            .ToList();

        var qualifyingReadyToIngest = !loaded.Value.GridConfirmed
            && await QualifyingReadyToIngestAsync(race, cancellationToken);

        return Result.Success(new NextRacePreviewResponse(
            query.Year,
            race.SessionKey,
            race.MeetingKey,
            race.MeetingName,
            race.CircuitShortName,
            race.CountryName,
            race.DateStart,
            race.SprintSessionKey,
            race.SprintDateStart,
            loaded.Value.GridConfirmed,
            drivers,
            qualifyingReadyToIngest));
    }

    /// <summary>
    /// Whether OpenF1 already has a qualifying grid the local database doesn't — checked only
    /// while the local grid is unconfirmed, and cached ~5 minutes so the page's 60-second poll
    /// doesn't hit OpenF1 on every tick. A failure here is logged and swallowed: the preview is
    /// the product, this flag is a nudge.
    /// </summary>
    private async Task<bool> QualifyingReadyToIngestAsync(UpcomingRace race, CancellationToken cancellationToken)
    {
        try
        {
            return await CachedQualifyingCheck.For(cache, race.MeetingKey, async ct =>
            {
                var sessions = await openF1.GetSessionsAsync(race.MeetingKey, ct);
                var qualifying = QualifyingSessionMatcher.Find(sessions, isSprint: false);

                if (qualifying is null)
                {
                    return false;
                }

                var grid = await openF1.GetStartingGridAsync(qualifying.SessionKey, ct);
                return grid.Count > 0;
            }, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Qualifying-ready check failed for meeting {MeetingKey}; assuming not ready.", race.MeetingKey);
            return false;
        }
    }

    private PreviewDriverResponse Describe(
        DriverEntry entry,
        DriverRaceFeature feature,
        IReadOnlyDictionary<int, DriverSeasonForm> history)
    {
        var probabilities = predictor.Predict(feature);

        return new PreviewDriverResponse(
            entry.DriverNumber,
            entry.FullName,
            entry.NameAcronym,
            entry.TeamName,
            entry.TeamColour,
            feature.GridPosition,
            feature.QualiGapToPole,
            history.ContainsKey(entry.DriverNumber),
            probabilities.PodiumProbability,
            probabilities.PointsProbability);
    }
}
