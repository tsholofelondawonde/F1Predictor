using System.Globalization;
using System.Text.Json;
using F1Predictor.Application.Abstractions.AI;
using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Abstractions.MachineLearning;
using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Analysis.ExplainDriverPrediction;
using F1Predictor.Application.Features.Predictions;
using F1Predictor.Application.Features.Predictions.PredictRace;
using F1Predictor.Domain.Predictions;
using F1Predictor.Domain.RaceData.Entities;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace F1Predictor.Application.Features.Analysis.ExplainRacePrediction;

/// <summary>
/// Explains one driver's prediction for an already-classified race, against what actually
/// happened, from the model's own per-feature contributions, with an optional AI-generated
/// narrative of those numbers layered on top.
/// </summary>
internal sealed class ExplainRacePredictionQueryHandler(
    IApplicationDbContext context,
    IRacePredictor predictor,
    IAiCapabilities ai,
    IChatClient chatClient,
    HybridCache cache,
    ILogger<ExplainRacePredictionQueryHandler> logger)
    : IQueryHandler<ExplainRacePredictionQuery, DriverExplanationResponse>
{
    private static readonly JsonSerializerOptions PromptJson = new(JsonSerializerDefaults.Web) { WriteIndented = false };

    // A classified race's inputs never change, unlike a next-race explanation whose grid can
    // still flip from projected to confirmed — so the narrative can be cached far longer.
    private static readonly TimeSpan NarrativeLifetime = TimeSpan.FromHours(1);

    public async Task<Result<DriverExplanationResponse>> Handle(ExplainRacePredictionQuery query, CancellationToken cancellationToken)
    {
        if (!predictor.ModelsAvailable)
        {
            return Result.Failure<DriverExplanationResponse>(PredictionErrors.ModelsNotTrained);
        }

        var loaded = await ClassifiedRace.LoadAsync(context, query.SessionKey, cancellationToken);
        if (loaded.IsFailure)
        {
            return Result.Failure<DriverExplanationResponse>(loaded.Error);
        }

        var race = loaded.Value;
        if (!race.Features.TryGetValue(query.DriverNumber, out var feature) || !race.Directory.TryGetValue(query.DriverNumber, out var entry))
        {
            return Result.Failure<DriverExplanationResponse>(AnalysisErrors.DriverNotInRace(query.SessionKey, query.DriverNumber));
        }

        var explanation = predictor.Explain(feature);
        var podium = TargetExplanationResponse.From(explanation.Podium);
        var points = TargetExplanationResponse.From(explanation.PointsFinish);

        var narrative = ai.ChatAvailable && query.IncludeNarrative
            ? await NarrativeAsync(query.SessionKey, feature, entry, podium, points, cancellationToken)
            : null;

        return Result.Success(new DriverExplanationResponse(
            query.SessionKey, race.MeetingName, GridConfirmed: true,
            entry.DriverNumber, entry.FullName, entry.NameAcronym, entry.TeamName, entry.TeamColour,
            podium, points,
            FinishPosition: feature.FinishPosition, ActualPodium: feature.Podium, ActualPointsFinish: feature.PointsFinish,
            narrative, narrative is null ? null : ai.Model));
    }

    /// <summary>
    /// One short paragraph, cached for an hour per driver/model — a classified race's inputs are
    /// fixed, so there is no grid state to key on the way the next-race version needs. A failure
    /// here is logged and swallowed: the structured explanation is the product, the prose is a
    /// garnish.
    /// </summary>
    private async Task<string?> NarrativeAsync(
        int sessionKey,
        DriverRaceFeature feature,
        DriverEntry entry,
        TargetExplanationResponse podium,
        TargetExplanationResponse points,
        CancellationToken cancellationToken)
    {
        var key = string.Create(CultureInfo.InvariantCulture, $"explain-race:{sessionKey}:{entry.DriverNumber}:{ai.Model}");

        try
        {
            var text = await CachedNarrative.For(cache, key, NarrativeLifetime, async ct =>
            {
                var prompt = JsonSerializer.Serialize(new
                {
                    driver = entry.NameAcronym,
                    team = entry.TeamName,
                    podium,
                    pointsFinish = points,
                    finishPosition = feature.FinishPosition,
                    actualPodium = feature.Podium,
                    actualPointsFinish = feature.PointsFinish
                }, PromptJson);

                var response = await chatClient.GetResponseAsync(
                    [new ChatMessage(ChatRole.System, AnalystPrompts.ExplainRaceSystem()), new ChatMessage(ChatRole.User, prompt)],
                    cancellationToken: ct);

                return response.Text.Trim();
            }, cancellationToken);

            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        // Deliberately every exception, not a list: OllamaSharp throws its own types (a model that
        // is not pulled, a malformed body) which this layer cannot name, and none of them should
        // cost the caller the contributions. The filter still lets the caller's own cancellation
        // propagate.
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Narrative generation failed for car {DriverNumber}; returning the structured explanation only.", entry.DriverNumber);
            return null;
        }
    }
}
