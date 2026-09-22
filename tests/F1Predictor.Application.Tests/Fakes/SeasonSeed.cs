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

    /// <summary>
    /// Adds a classified race in a different season, with a session key higher than any of
    /// <see cref="SeedNextRaceAsync"/>'s — so a staleness check that forgets to scope by year
    /// would wrongly treat it as the latest classified session.
    /// </summary>
    public static async Task SeedOtherSeasonClassifiedRaceAsync(ApplicationDbContext db, int year, int sessionKey)
    {
        db.Meetings.Add(new Meeting { MeetingKey = sessionKey, Year = year, MeetingName = "Other Season GP", CircuitShortName = "Z", CountryName = "Z", DateStart = DateTimeOffset.UtcNow.AddDays(-30) });
        db.RaceSessions.Add(new RaceSession { SessionKey = sessionKey, MeetingKey = sessionKey, SessionName = "Race", SessionType = "Race", DateStart = DateTimeOffset.UtcNow.AddDays(-30), IsClassified = true });

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// A fully classified, non-sprint race with all six raw inputs <c>RaceFactSheet.Build</c>
    /// needs — meeting, session, results, grid, pit stops, weather and a driver directory — for
    /// tests of <c>IndexRaceCommandHandler</c> and the similarity-search handlers that hydrate
    /// off a real session/meeting join.
    /// </summary>
    public static async Task SeedClassifiedRaceAsync(
        ApplicationDbContext db, int sessionKey, int meetingKey, int year,
        string meetingName, string circuitShortName, DateTimeOffset dateStart)
    {
        db.Meetings.Add(new Meeting { MeetingKey = meetingKey, Year = year, MeetingName = meetingName, CircuitShortName = circuitShortName, CountryName = circuitShortName, DateStart = dateStart });
        db.RaceSessions.Add(new RaceSession { SessionKey = sessionKey, MeetingKey = meetingKey, SessionName = "Race", SessionType = "Race", DateStart = dateStart, IsClassified = true });

        for (var n = 1; n <= 3; n++)
        {
            db.DriverEntries.Add(new DriverEntry { SessionKey = sessionKey, DriverNumber = n, FullName = $"Driver {n}", NameAcronym = $"D0{n}", TeamName = "Team", TeamColour = "FF0000" });
            db.StartingGridEntries.Add(new StartingGridEntry { SessionKey = sessionKey, DriverNumber = n, Position = n, LapDuration = 80.0 + n });
            db.SessionResultEntries.Add(new SessionResultEntry { SessionKey = sessionKey, DriverNumber = n, Position = n, Points = n switch { 1 => 25, 2 => 18, _ => 15 } });
            db.PitStopEntries.Add(new PitStopEntry { SessionKey = sessionKey, DriverNumber = n, StopDuration = 2.5 + n * 0.1 });
        }

        db.WeatherReadings.Add(new WeatherReading { SessionKey = sessionKey, Rainfall = 0, TrackTemperature = 40 });

        await db.SaveChangesAsync();
    }
}
