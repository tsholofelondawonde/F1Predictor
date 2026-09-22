using F1Predictor.Application.Abstractions.AI;
using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Analysis.GenerateRacePreview;
using F1Predictor.Application.Features.Analysis.IndexRace;
using F1Predictor.Application.Features.Predictions.PreviewNextRace;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace F1Predictor.Application.Features.Analysis.RefreshAnalysis;

/// <summary>
/// Re-generates the AI race preview for whichever seasons' next Grand Prix has moved on since it
/// was last written — a real grid replacing a projected one, or a newer race being classified —
/// and indexes every classified, non-sprint race in scope for similarity search, so neither a
/// preview refresh nor a race index depends on a human noticing and pressing a button. Called by
/// the ingestion coordinator (see <c>SeasonIngestionCoordinatorJob</c>) right after a run that
/// stored a grid or classified a race.
/// </summary>
/// <remarks>
/// The two steps are independent: chat and embeddings are separate <see cref="IAiCapabilities"/>
/// switches, so one being off does not skip the other. Orchestrates by calling the existing
/// <see cref="GenerateRacePreviewCommand"/> and <see cref="IndexRaceCommand"/> handlers rather
/// than duplicating either's logic. Indexing needs no staleness check of its own —
/// <see cref="IndexRaceCommandHandler"/>'s own content-hash short-circuit already makes it cheap
/// to call for every classified race on every refresh.
/// </remarks>
internal sealed class RefreshAnalysisCommandHandler(
    IApplicationDbContext context,
    ICommandHandler<GenerateRacePreviewCommand, RacePreviewResponse> generatePreview,
    ICommandHandler<IndexRaceCommand, IndexRaceResponse> indexRace,
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
            skipped = years.Count;
        }
        else
        {
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
        }

        var indexed = await IndexClassifiedRacesAsync(years, ai.EmbeddingsAvailable, notes, cancellationToken);

        return Result.Success(new RefreshAnalysisResponse(generated, indexed, skipped, notes));
    }

    /// <summary>
    /// Indexes every classified, non-sprint race across <paramref name="years"/> — sprints are
    /// never previewed or explained either, per the ml-pipeline rule that a sprint's shorter
    /// distance and different points scale make it a poor fit for anything the models or the
    /// analyst reason about. Cheap to call unconditionally: <see cref="IndexRaceCommandHandler"/>
    /// skips the embedding call itself for a race whose fact sheet has not changed.
    /// </summary>
    private async Task<int> IndexClassifiedRacesAsync(
        IReadOnlyList<int> years, bool embeddingsAvailable, List<string> notes, CancellationToken cancellationToken)
    {
        if (!embeddingsAvailable)
        {
            notes.Add("Embeddings unavailable — race indexing skipped.");
            return 0;
        }

        var sessionKeys = await (
            from session in context.RaceSessions
            join meeting in context.Meetings on session.MeetingKey equals meeting.MeetingKey
            where session.IsClassified && !session.IsSprint && years.Contains(meeting.Year)
            select session.SessionKey)
            .ToListAsync(cancellationToken);

        var indexed = 0;
        foreach (var sessionKey in sessionKeys)
        {
            var result = await indexRace.Handle(new IndexRaceCommand { SessionKey = sessionKey }, cancellationToken);
            if (result.IsSuccess && result.Value.Reindexed)
            {
                indexed++;
            }
        }

        return indexed;
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
