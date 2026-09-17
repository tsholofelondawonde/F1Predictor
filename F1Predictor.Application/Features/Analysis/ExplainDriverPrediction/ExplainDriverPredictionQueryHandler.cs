using System.Globalization;
using System.Text.Json;
using F1Predictor.Application.Abstractions.AI;
using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Abstractions.MachineLearning;
using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Predictions;
using F1Predictor.Application.Features.Predictions.PreviewNextRace;
using F1Predictor.Domain.RaceData.Entities;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace F1Predictor.Application.Features.Analysis.ExplainDriverPrediction;

/// <summary>
/// Explains one driver's next-race prediction from the model's own per-feature contributions,
/// with an optional AI-generated narrative of those numbers layered on top.
/// </summary>
internal sealed class ExplainDriverPredictionQueryHandler(
    IApplicationDbContext context,
    IRacePredictor predictor,
    IAiCapabilities ai,
    IChatClient chatClient,
    HybridCache cache,
    ILogger<ExplainDriverPredictionQueryHandler> logger)
    : IQueryHandler<ExplainDriverPredictionQuery, DriverExplanationResponse>
{
    private static readonly JsonSerializerOptions PromptJson = new(JsonSerializerDefaults.Web) { WriteIndented = false };

    public async Task<Result<DriverExplanationResponse>> Handle(ExplainDriverPredictionQuery query, CancellationToken cancellationToken)
    {
        if (!predictor.ModelsAvailable)
        {
            return Result.Failure<DriverExplanationResponse>(PredictionErrors.ModelsNotTrained);
        }

        var loaded = await NextRaceContext.LoadAsync(context, query.Year, cancellationToken);
        if (loaded.IsFailure)
        {
            return Result.Failure<DriverExplanationResponse>(loaded.Error);
        }

        var next = loaded.Value;
        var entry = next.Entries.FirstOrDefault(e => e.DriverNumber == query.DriverNumber);
        if (entry is null)
        {
            return Result.Failure<DriverExplanationResponse>(AnalysisErrors.DriverNotEntered(query.DriverNumber));
        }

        var explanation = predictor.Explain(next.Features[entry.DriverNumber]);
        var podium = TargetExplanationResponse.From(explanation.Podium);
        var points = TargetExplanationResponse.From(explanation.PointsFinish);

        var narrative = ai.ChatAvailable
            ? await NarrativeAsync(next, entry, podium, points, cancellationToken)
            : null;

        return Result.Success(new DriverExplanationResponse(
            next.Race.SessionKey, next.Race.MeetingName, next.GridConfirmed,
            entry.DriverNumber, entry.FullName, entry.NameAcronym, entry.TeamName, entry.TeamColour,
            podium, points,
            FinishPosition: null, ActualPodium: null, ActualPointsFinish: null,
            narrative, narrative is null ? null : ai.Model));
    }

    /// <summary>
    /// One short paragraph, cached for ten minutes per driver/grid state/model. A failure here is
    /// logged and swallowed: the structured explanation is the product, the prose is a garnish.
    /// </summary>
    private async Task<string?> NarrativeAsync(NextRaceContext next, DriverEntry entry, TargetExplanationResponse podium, TargetExplanationResponse points, CancellationToken cancellationToken)
    {
        var key = string.Create(CultureInfo.InvariantCulture, $"explain:{next.Race.SessionKey}:{entry.DriverNumber}:{next.GridConfirmed}:{ai.Model}");

        try
        {
            var text = await CachedNarrative.For(cache, key, async ct =>
            {
                var prompt = JsonSerializer.Serialize(new
                {
                    driver = entry.NameAcronym,
                    team = entry.TeamName,
                    gridConfirmed = next.GridConfirmed,
                    podium,
                    pointsFinish = points,
                    seasonForm = next.History.TryGetValue(entry.DriverNumber, out var form)
                        ? new
                        {
                            averageGridPosition = form.AverageGridPosition,
                            averageQualiGap = form.AverageQualiGap,
                            averagePitStops = form.AveragePitStops,
                            averagePitDuration = form.AveragePitDuration
                        }
                        : null
                }, PromptJson);

                var response = await chatClient.GetResponseAsync(
                    [new ChatMessage(ChatRole.System, AnalystPrompts.DriverExplanationSystem()), new ChatMessage(ChatRole.User, prompt)],
                    cancellationToken: ct);

                return response.Text.Trim();
            }, cancellationToken);

            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException
            && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Narrative generation failed for car {DriverNumber}; returning the structured explanation only.", entry.DriverNumber);
            return null;
        }
    }
}
