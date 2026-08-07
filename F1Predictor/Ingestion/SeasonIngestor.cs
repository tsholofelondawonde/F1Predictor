using F1Predictor.Data;
using F1Predictor.OpenF1;
using Microsoft.EntityFrameworkCore;

namespace F1Predictor.Ingestion;

public class SeasonIngestor
{
    private static readonly TimeSpan RequestDelay = TimeSpan.FromMilliseconds(500);

    private readonly OpenF1Client _client;
    private readonly AppDbContext _db;

    public SeasonIngestor(OpenF1Client client, AppDbContext db)
    {
        _client = client;
        _db = db;
    }

    public async Task IngestSeasonAsync(int year)
    {
        var meetings = await _client.GetMeetingsAsync(year);
        Console.WriteLine($"Found {meetings.Count} meetings for {year}.");

        foreach (var meeting in meetings)
        {
            await UpsertMeetingAsync(meeting);

            var sessions = await _client.GetSessionsAsync(meeting.MeetingKey);
            var raceSession = sessions.FirstOrDefault(s => s.SessionType == "Race" && s.SessionName == "Race");
            if (raceSession is null)
            {
                Console.WriteLine($"  {meeting.MeetingName}: no race session yet, skipping.");
                continue;
            }

            var alreadyIngested = await _db.RaceSessions.AnyAsync(s => s.SessionKey == raceSession.SessionKey);
            if (alreadyIngested)
            {
                Console.WriteLine($"  {meeting.MeetingName}: already ingested, skipping.");
                continue;
            }

            var results = await _client.GetSessionResultAsync(raceSession.SessionKey);
            await Task.Delay(RequestDelay);
            if (results.Count == 0)
            {
                Console.WriteLine($"  {meeting.MeetingName}: race not yet classified, skipping.");
                continue;
            }

            var qualiSession = sessions.FirstOrDefault(s => s.SessionType == "Qualifying" && s.SessionName == "Qualifying");

            var grid = qualiSession is null
                ? []
                : await _client.GetStartingGridAsync(qualiSession.SessionKey);
            await Task.Delay(RequestDelay);

            var pits = await _client.GetPitStopsAsync(raceSession.SessionKey);
            await Task.Delay(RequestDelay);

            var weather = await _client.GetWeatherAsync(raceSession.SessionKey);
            await Task.Delay(RequestDelay);

            _db.RaceSessions.Add(new RaceSession
            {
                SessionKey = raceSession.SessionKey,
                MeetingKey = raceSession.MeetingKey,
                SessionName = raceSession.SessionName,
                SessionType = raceSession.SessionType,
                DateStart = raceSession.DateStart,
                QualifyingSessionKey = qualiSession?.SessionKey
            });

            foreach (var g in grid)
            {
                _db.StartingGridEntries.Add(new StartingGridEntry
                {
                    SessionKey = raceSession.SessionKey,
                    DriverNumber = g.DriverNumber,
                    Position = g.Position,
                    LapDuration = g.LapDuration
                });
            }

            foreach (var r in results)
            {
                _db.SessionResultEntries.Add(new SessionResultEntry
                {
                    SessionKey = raceSession.SessionKey,
                    DriverNumber = r.DriverNumber,
                    Position = r.Position,
                    Dnf = r.Dnf,
                    Dns = r.Dns,
                    Dsq = r.Dsq
                });
            }

            foreach (var p in pits)
            {
                _db.PitStopEntries.Add(new PitStopEntry
                {
                    SessionKey = raceSession.SessionKey,
                    DriverNumber = p.DriverNumber,
                    StopDuration = p.StopDuration,
                    LaneDuration = p.LaneDuration
                });
            }

            foreach (var w in weather)
            {
                _db.WeatherReadings.Add(new WeatherReading
                {
                    SessionKey = raceSession.SessionKey,
                    Rainfall = w.Rainfall,
                    TrackTemperature = w.TrackTemperature
                });
            }

            await _db.SaveChangesAsync();
            Console.WriteLine($"  {meeting.MeetingName}: ingested ({results.Count} results, {grid.Count} grid rows, {pits.Count} pit stops, {weather.Count} weather readings).");
        }
    }

    private async Task UpsertMeetingAsync(MeetingDto meeting)
    {
        var existing = await _db.Meetings.FindAsync(meeting.MeetingKey);
        if (existing is null)
        {
            _db.Meetings.Add(new Meeting
            {
                MeetingKey = meeting.MeetingKey,
                Year = meeting.Year,
                CircuitShortName = meeting.CircuitShortName,
                CountryName = meeting.CountryName,
                MeetingName = meeting.MeetingName,
                DateStart = meeting.DateStart
            });
        }
        else
        {
            existing.Year = meeting.Year;
            existing.CircuitShortName = meeting.CircuitShortName;
            existing.CountryName = meeting.CountryName;
            existing.MeetingName = meeting.MeetingName;
            existing.DateStart = meeting.DateStart;
        }

        await _db.SaveChangesAsync();
    }
}
