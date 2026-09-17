using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Analysis.GenerateRacePreview;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace F1Predictor.Application.Features.Analysis.GetRacePreview;

/// <summary>
/// Serves the last generated preview for a race session, flagging it stale when the grid it was
/// projected from has since been published, or a newer session has been classified.
/// </summary>
internal sealed class GetRacePreviewQueryHandler(IApplicationDbContext context) : IQueryHandler<GetRacePreviewQuery, RacePreviewResponse>
{
    public async Task<Result<RacePreviewResponse>> Handle(GetRacePreviewQuery query, CancellationToken cancellationToken)
    {
        var narrative = await context.RacePreviewNarratives.AsNoTracking().FirstOrDefaultAsync(n => n.SessionKey == query.SessionKey, cancellationToken);
        if (narrative is null) return Result.Failure<RacePreviewResponse>(AnalysisErrors.PreviewNotFound(query.SessionKey));

        var race = await (from session in context.RaceSessions
                          join meeting in context.Meetings on session.MeetingKey equals meeting.MeetingKey
                          where session.SessionKey == query.SessionKey
                          select new { meeting.MeetingName, meeting.Year }).FirstOrDefaultAsync(cancellationToken);

        var gridNowPublished = !narrative.GridConfirmed
            && await context.StartingGridEntries.AnyAsync(g => g.SessionKey == query.SessionKey, cancellationToken);

        // BasedOnLatestClassifiedSessionKey was set from SeasonChampionship.LatestClassifiedSessionKey,
        // which only looks at the previewed race's own season, so this comparison must be scoped the
        // same way — otherwise a classified race in a different season would falsely flag every
        // preview stale.
        int? latestClassified = null;
        if (race is not null)
        {
            latestClassified = await (from session in context.RaceSessions
                                      join meeting in context.Meetings on session.MeetingKey equals meeting.MeetingKey
                                      where meeting.Year == race.Year && session.IsClassified
                                      select (int?)session.SessionKey).MaxAsync(cancellationToken);
        }

        // A null BasedOnLatestClassifiedSessionKey means the preview was written before any race in
        // the season was classified; the first classified result then makes it stale too.
        var resultsMovedOn = latestClassified is { } latest
            && (narrative.BasedOnLatestClassifiedSessionKey is null || latest > narrative.BasedOnLatestClassifiedSessionKey);

        return Result.Success(new RacePreviewResponse(
            narrative.SessionKey, race?.MeetingName ?? "Unknown meeting", narrative.GridConfirmed, narrative.Model, narrative.GeneratedAt,
            narrative.Headline, narrative.Content, Stale: gridNowPublished || resultsMovedOn));
    }
}
