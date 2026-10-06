using System.Globalization;
using F1Predictor.Application.Abstractions.AI;
using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Abstractions.MachineLearning;
using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Championship;
using F1Predictor.Application.Features.Predictions;
using F1Predictor.Application.Features.Predictions.PreviewNextRace;
using F1Predictor.Domain.Analysis.Entities;
using F1Predictor.Domain.Championship;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace F1Predictor.Application.Features.Analysis.GenerateRacePreview;

/// <summary>
/// Writes (or rewrites) an AI narrative of the next Grand Prix from the same rounded model
/// output an explanation would show, and persists it — see <c>RacePreviewNarrative</c>.
/// </summary>
internal sealed class GenerateRacePreviewCommandHandler(
    IApplicationDbContext context, IRacePredictor predictor, IAiCapabilities ai, IChatClient chatClient,
    HybridCache cache, IDateTimeProvider clock, ILogger<GenerateRacePreviewCommandHandler> logger)
    : ICommandHandler<GenerateRacePreviewCommand, RacePreviewResponse>
{
    public async Task<Result<RacePreviewResponse>> Handle(GenerateRacePreviewCommand command, CancellationToken cancellationToken)
    {
        if (!predictor.ModelsAvailable)
        {
            return Result.Failure<RacePreviewResponse>(PredictionErrors.ModelsNotTrained);
        }

        if (!ai.ChatAvailable)
        {
            return Result.Failure<RacePreviewResponse>(AnalysisErrors.AiUnavailable);
        }

        var year = await (from session in context.RaceSessions
                          join meeting in context.Meetings on session.MeetingKey equals meeting.MeetingKey
                          where session.SessionKey == command.SessionKey
                          select (int?)meeting.Year).FirstOrDefaultAsync(cancellationToken);
        if (year is null)
        {
            return Result.Failure<RacePreviewResponse>(AnalysisErrors.RaceNotFound(command.SessionKey));
        }

        var loaded = await NextRaceContext.LoadAsync(context, year.Value, cancellationToken);
        if (loaded.IsFailure)
        {
            return Result.Failure<RacePreviewResponse>(loaded.Error);
        }

        var next = loaded.Value;
        if (next.Race.SessionKey != command.SessionKey)
        {
            return Result.Failure<RacePreviewResponse>(AnalysisErrors.NotNextRace(command.SessionKey));
        }

        var season = await SeasonChampionship.LoadAsync(context, year.Value, cancellationToken);
        var previewContext = await BuildContextAsync(next, season, cancellationToken);

        var written = await WriteAsync(previewContext, command.SessionKey, cancellationToken);
        if (written.IsFailure)
        {
            return Result.Failure<RacePreviewResponse>(written.Error);
        }

        var narrative = await context.RacePreviewNarratives.FirstOrDefaultAsync(n => n.SessionKey == command.SessionKey, cancellationToken)
            ?? context.RacePreviewNarratives.Add(new RacePreviewNarrative { SessionKey = command.SessionKey }).Entity;

        narrative.GridConfirmed = next.GridConfirmed;
        narrative.BasedOnLatestClassifiedSessionKey = season.LatestClassifiedSessionKey == 0 ? null : season.LatestClassifiedSessionKey;
        // Both columns are bounded (see RacePreviewNarrativeConfiguration); a model can write a
        // heading longer than the column, and that must not fail the whole generation at SaveChanges.
        narrative.Model = Truncate(ai.Model ?? ai.Provider, RacePreviewNarrative.ModelMaxLength);
        narrative.GeneratedAt = new DateTimeOffset(clock.UtcNow, TimeSpan.Zero);
        narrative.Headline = Truncate(HeadlineOf(written.Value), RacePreviewNarrative.HeadlineMaxLength);
        narrative.Content = written.Value;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(new RacePreviewResponse(
            narrative.SessionKey, next.Race.MeetingName, narrative.GridConfirmed, narrative.Model,
            narrative.GeneratedAt, narrative.Headline, narrative.Content, Stale: false));
    }

    /// <summary>
    /// One call to the provider, with both ways it can come back empty-handed as failures: no
    /// text, or an exception. The exception is logged here and never reaches the client.
    /// </summary>
    private async Task<Result<string>> WriteAsync(PreviewContext previewContext, int sessionKey, CancellationToken cancellationToken)
    {
        string content;
        try
        {
            var response = await chatClient.GetResponseAsync(
                [new ChatMessage(ChatRole.System, AnalystPrompts.RacePreviewSystem()), new ChatMessage(ChatRole.User, previewContext.ToPromptJson())],
                cancellationToken: cancellationToken);

            content = response.Text.Trim();
        }
        // Deliberately every exception, not a list: OllamaSharp throws its own types (a model that
        // is not pulled, a malformed body) which this layer cannot name, and a provider failure
        // is a business outcome to report, not a 500. The filter still lets the caller's own
        // cancellation propagate.
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "The AI provider failed while writing the preview for session {SessionKey}.", sessionKey);
            return Result.Failure<string>(AnalysisErrors.ProviderFailed);
        }

        return content.Length == 0
            ? Result.Failure<string>(AnalysisErrors.ModelRefused)
            : Result.Success(content);
    }

    private async Task<PreviewContext> BuildContextAsync(NextRaceContext next, SeasonChampionship season, CancellationToken cancellationToken)
    {
        var drivers = next.Entries.Select(entry =>
        {
            var explanation = predictor.Explain(next.Features[entry.DriverNumber]);
            var podium = TargetExplanationResponse.From(explanation.Podium);
            return new PreviewDriverSignal(entry.NameAcronym, entry.TeamName, (int)next.Features[entry.DriverNumber].GridPosition,
                podium.Probability, explanation.PointsFinish.Probability,
                [.. podium.Contributions.Select(c => new PreviewSignal(c.Feature, c.Direction, c.Contribution))]);
        }).ToList();

        var standings = season.HasResults
            ? season.Standings.Drivers.Select(d => new PreviewStandingRow(d.NameAcronym, d.Points, d.Wins)).ToList()
            : [];

        List<PreviewOddsRow> odds = [];
        if (season.HasResults && !season.SeasonComplete)
        {
            var forecast = await CachedForecast.For(cache, season, ChampionshipSimulator.DefaultSimulations, cancellationToken);
            var acronyms = season.Standings.Drivers.ToDictionary(d => d.DriverNumber, d => d.NameAcronym);
            odds = forecast.Drivers.OrderByDescending(d => d.Odds.TitleProbability)
                .Select(d => new PreviewOddsRow(acronyms.GetValueOrDefault(d.DriverNumber, d.DriverNumber.ToString(CultureInfo.InvariantCulture)), (float)d.Odds.TitleProbability)).ToList();
        }

        return PreviewContext.Build(
            new PreviewRace(next.Race.MeetingName, next.Race.CircuitShortName, next.Race.CountryName, next.Race.DateStart, next.GridConfirmed),
            drivers, standings, odds);
    }

    private static string Truncate(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];

    /// <summary>The first markdown heading, stripped; otherwise the first non-empty line.</summary>
    internal static string HeadlineOf(string content)
    {
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var heading = lines.FirstOrDefault(l => l.StartsWith('#')) ?? lines.FirstOrDefault() ?? "";
        return heading.TrimStart('#').Trim();
    }
}
