using F1Predictor.Application.Abstractions.AI;
using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Analysis.GenerateRacePreview;
using F1Predictor.Application.Features.Predictions.PreviewNextRace;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace F1Predictor.Application.Features.Analysis.RefreshAnalysis;

/// <summary>
/// Re-generates the AI race preview for whichever seasons' next Grand Prix has moved on since it
/// was last written — a real grid replacing a projected one, or a newer race being classified —
/// so a preview refresh does not depend on a human noticing and pressing the button. Called by
/// the ingestion coordinator (see <c>SeasonIngestionCoordinatorJob</c>) right after a run that
/// stored a grid or classified a race.
/// </summary>
/// <remarks>
/// Preview step only: <see cref="RefreshAnalysisResponse.RacesIndexed"/> stays 0 here. A later
/// task extends this handler to also index newly classified races for semantic search.
/// Orchestrates by calling the existing <see cref="GenerateRacePreviewCommand"/> handler rather
/// than duplicating its generation logic.
/// </remarks>
internal sealed class RefreshAnalysisCommandHandler(
    IApplicationDbContext context,
    ICommandHandler<GenerateRacePreviewCommand, RacePreviewResponse> generatePreview,
    IAiCapabilities ai)
    : ICommandHandler<RefreshAnalysisCommand, RefreshAnalysisResponse>
{
    public async Task<Result<RefreshAnalysisResponse>> Handle(RefreshAnalysisCommand command, CancellationToken cancellationToken)
    {
        var years = command.Year is { } year
            ? [year]
            : await context.Meetings.Select(m => m.Year).Distinct().ToListAsync(cancellationToken);

        var generated = 0;
        var skipped = 0;
        List<string> notes = [];

        if (!ai.ChatAvailable)
        {
            notes.Add("Chat unavailable — preview refresh skipped for all years.");
            return Result.Success(new RefreshAnalysisResponse(0, RacesIndexed: 0, years.Count, notes));
        }

        foreach (var seasonYear in years)
        {
            var (outcome, errorCode) = await RefreshYearAsync(seasonYear, cancellationToken);

            switch (outcome)
            {
                case RefreshOutcome.Generated:
                    generated++;
                    break;
                case RefreshOutcome.Skipped:
                    skipped++;
                    break;
                case RefreshOutcome.Failed:
                    notes.Add($"{seasonYear}: preview generation failed ({errorCode}).");
                    break;
            }
        }

        return Result.Success(new RefreshAnalysisResponse(generated, RacesIndexed: 0, skipped, notes));
    }

    /// <summary>
    /// Refreshes one season's next-race preview if it needs it: missing, a projected grid that
    /// has since been confirmed, or a newer classified race than the one the preview was written
    /// from. A season with no upcoming race, or an already-fresh preview, is a skip, not a failure.
    /// </summary>
    private async Task<(RefreshOutcome Outcome, string? ErrorCode)> RefreshYearAsync(int seasonYear, CancellationToken cancellationToken)
    {
        var loaded = await NextRaceContext.LoadAsync(context, seasonYear, cancellationToken);
        if (loaded.IsFailure)
        {
            // No upcoming race this season — nothing to preview.
            return (RefreshOutcome.Skipped, null);
        }

        var next = loaded.Value;

        var existing = await context.RacePreviewNarratives
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.SessionKey == next.Race.SessionKey, cancellationToken);

        // Scoped to seasonYear, matching how BasedOnLatestClassifiedSessionKey was set
        // (SeasonChampionship.LatestClassifiedSessionKey only looks at that one season — see
        // GetRacePreviewQueryHandler for the same rule) — otherwise a classified race in a
        // different season would falsely flag this season's preview stale.
        var latestClassified = await (
            from session in context.RaceSessions
            join meeting in context.Meetings on session.MeetingKey equals meeting.MeetingKey
            where meeting.Year == seasonYear && session.IsClassified
            select (int?)session.SessionKey)
            .MaxAsync(cancellationToken);

        var shouldRegenerate = existing is null
            || (!existing.GridConfirmed && next.GridConfirmed)
            || existing.BasedOnLatestClassifiedSessionKey != latestClassified;

        if (!shouldRegenerate)
        {
            return (RefreshOutcome.Skipped, null);
        }

        var result = await generatePreview.Handle(
            new GenerateRacePreviewCommand { SessionKey = next.Race.SessionKey }, cancellationToken);

        return result.IsSuccess
            ? (RefreshOutcome.Generated, null)
            : (RefreshOutcome.Failed, result.Error.Code);
    }

    /// <summary>What happened to one season's preview refresh attempt.</summary>
    private enum RefreshOutcome
    {
        Generated,
        Skipped,
        Failed
    }
}
