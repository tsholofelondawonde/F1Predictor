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

        var meetingName = await (from session in context.RaceSessions
                                 join meeting in context.Meetings on session.MeetingKey equals meeting.MeetingKey
                                 where session.SessionKey == query.SessionKey
                                 select meeting.MeetingName).FirstOrDefaultAsync(cancellationToken) ?? "Unknown meeting";

        var gridNowPublished = !narrative.GridConfirmed
            && await context.StartingGridEntries.AnyAsync(g => g.SessionKey == query.SessionKey, cancellationToken);

        var latestClassified = await context.RaceSessions.Where(s => s.IsClassified).MaxAsync(s => (int?)s.SessionKey, cancellationToken);
        var resultsMovedOn = narrative.BasedOnLatestClassifiedSessionKey is { } basedOn && latestClassified > basedOn;

        return Result.Success(new RacePreviewResponse(
            narrative.SessionKey, meetingName, narrative.GridConfirmed, narrative.Model, narrative.GeneratedAt,
            narrative.Headline, narrative.Content, Stale: gridNowPublished || resultsMovedOn));
    }
}
