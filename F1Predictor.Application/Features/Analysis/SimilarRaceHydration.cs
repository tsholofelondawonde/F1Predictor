using F1Predictor.Application.Abstractions.AI;
using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Features.Analysis.FindSimilarRaces;
using Microsoft.EntityFrameworkCore;

namespace F1Predictor.Application.Features.Analysis;

/// <summary>
/// Shared by <c>FindSimilarRacesQueryHandler</c> and <c>SearchRacesQueryHandler</c>: neither
/// returns anything but a session key and a fact-sheet excerpt, so both need the same join back
/// to <c>Meeting</c>/<c>RaceSession</c> to fill in a <see cref="SimilarRace"/> row's year, circuit
/// and date. A per-area shared file, per this codebase's convention for helpers used by more than
/// one use case.
/// </summary>
internal static class SimilarRaceHydration
{
    public static async Task<string> MeetingNameFor(IApplicationDbContext context, int sessionKey, CancellationToken cancellationToken) =>
        await (
            from session in context.RaceSessions
            join meeting in context.Meetings on session.MeetingKey equals meeting.MeetingKey
            where session.SessionKey == sessionKey
            select meeting.MeetingName)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken) ?? "Unknown meeting";

    /// <summary>
    /// Joins each match back to its meeting/session, preserving <paramref name="matches"/>'
    /// similarity-ranked order. A match whose session has since been removed is silently dropped
    /// rather than failing the whole call.
    /// </summary>
    public static async Task<IReadOnlyList<SimilarRace>> HydrateAsync(
        IApplicationDbContext context, IReadOnlyList<RaceEmbeddingMatch> matches, CancellationToken cancellationToken)
    {
        if (matches.Count == 0)
        {
            return [];
        }

        var sessionKeys = matches.Select(m => m.SessionKey).ToList();

        var rows = await (
            from session in context.RaceSessions
            join meeting in context.Meetings on session.MeetingKey equals meeting.MeetingKey
            where sessionKeys.Contains(session.SessionKey)
            select new { session.SessionKey, meeting.Year, meeting.MeetingName, meeting.CircuitShortName, session.DateStart })
            .AsNoTracking()
            .ToDictionaryAsync(row => row.SessionKey, cancellationToken);

        return matches
            .Where(match => rows.ContainsKey(match.SessionKey))
            .Select(match =>
            {
                var row = rows[match.SessionKey];
                return new SimilarRace(match.SessionKey, row.Year, row.MeetingName, row.CircuitShortName, row.DateStart, match.Similarity, match.Text);
            })
            .ToList();
    }
}
