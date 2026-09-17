using F1Predictor.Domain.Predictions;
using F1Predictor.Domain.RaceData.Entities;
using F1Predictor.Infrastructure.Database;

namespace F1Predictor.Application.Tests.Fakes;

internal static class SeasonSeed
{
    private const int Year = 2026;

    public static async Task SeedNextRaceAsync(ApplicationDbContext db, bool withGrid)
    {
        db.Meetings.Add(new Meeting { MeetingKey = 1, Year = Year, MeetingName = "Past GP", CircuitShortName = "A", CountryName = "X", DateStart = DateTimeOffset.UtcNow.AddDays(-14) });
        db.Meetings.Add(new Meeting { MeetingKey = 2, Year = Year, MeetingName = "Next GP", CircuitShortName = "B", CountryName = "Y", DateStart = DateTimeOffset.UtcNow.AddDays(7) });
        db.RaceSessions.Add(new RaceSession { SessionKey = 10, MeetingKey = 1, SessionName = "Race", SessionType = "Race", DateStart = DateTimeOffset.UtcNow.AddDays(-14), IsClassified = true });
        db.RaceSessions.Add(new RaceSession { SessionKey = 20, MeetingKey = 2, SessionName = "Race", SessionType = "Race", DateStart = DateTimeOffset.UtcNow.AddDays(7) });

        for (var n = 1; n <= 3; n++)
        {
            db.DriverEntries.Add(new DriverEntry { SessionKey = 20, DriverNumber = n, FullName = $"Driver {n}", NameAcronym = $"D0{n}", TeamName = "Team", TeamColour = "FF0000" });
            db.DriverRaceFeatures.Add(new DriverRaceFeature { SessionKey = 10, DriverNumber = n, GridPosition = n, QualiGapToPole = n * 0.2f, PitStopCount = 2, AvgPitStopDuration = 23, FinishPosition = n, Podium = true, PointsFinish = true });
        }

        if (withGrid)
        {
            db.StartingGridEntries.Add(new StartingGridEntry { SessionKey = 20, DriverNumber = 3, Position = 1, LapDuration = 80.0 });
            db.StartingGridEntries.Add(new StartingGridEntry { SessionKey = 20, DriverNumber = 1, Position = 2, LapDuration = 80.4 });
            db.StartingGridEntries.Add(new StartingGridEntry { SessionKey = 20, DriverNumber = 2, Position = 3, LapDuration = 80.9 });
        }

        await db.SaveChangesAsync();
    }
}
