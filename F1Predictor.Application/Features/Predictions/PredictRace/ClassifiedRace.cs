using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Domain.Predictions;
using F1Predictor.Domain.RaceData.Entities;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace F1Predictor.Application.Features.Predictions.PredictRace;

/// <summary>
/// Everything a classified-race use case needs, loaded once: the meeting name, the driver
/// directory and one persisted feature row per driver, keyed by car number. Shared by the
/// race-predictions query and the classified-race explanation query — same spirit as
/// <c>NextRaceContext</c> for the upcoming race.
/// </summary>
internal sealed record ClassifiedRace(
    string MeetingName,
    IReadOnlyDictionary<int, DriverEntry> Directory,
    IReadOnlyDictionary<int, DriverRaceFeature> Features)
{
    public static async Task<Result<ClassifiedRace>> LoadAsync(
        IApplicationDbContext context,
        int sessionKey,
        CancellationToken cancellationToken)
    {
        var features = await context.DriverRaceFeatures
            .Where(f => f.SessionKey == sessionKey)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (features.Count == 0)
        {
            return Result.Failure<ClassifiedRace>(PredictionErrors.RaceNotFound(sessionKey));
        }

        var meetingName = await (
            from session in context.RaceSessions
            join meeting in context.Meetings on session.MeetingKey equals meeting.MeetingKey
            where session.SessionKey == sessionKey
            select meeting.MeetingName)
            .FirstOrDefaultAsync(cancellationToken) ?? "Unknown meeting";

        var directory = await context.DriverEntries
            .Where(d => d.SessionKey == sessionKey)
            .AsNoTracking()
            .ToDictionaryAsync(d => d.DriverNumber, cancellationToken);

        return Result.Success(new ClassifiedRace(
            meetingName,
            directory,
            features.ToDictionary(f => f.DriverNumber)));
    }
}
