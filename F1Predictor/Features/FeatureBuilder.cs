using F1Predictor.Data;
using Microsoft.EntityFrameworkCore;

namespace F1Predictor.Features;

public class FeatureBuilder
{
    private const double MissingLapTimeSentinel = 99;

    private readonly AppDbContext _db;

    public FeatureBuilder(AppDbContext db)
    {
        _db = db;
    }

    public async Task BuildAsync()
    {
        _db.DriverRaceFeatures.RemoveRange(_db.DriverRaceFeatures);
        await _db.SaveChangesAsync();

        var sessions = await _db.RaceSessions.ToListAsync();

        foreach (var session in sessions)
        {
            var grid = await _db.StartingGridEntries.Where(g => g.SessionKey == session.SessionKey).ToListAsync();
            var results = await _db.SessionResultEntries.Where(r => r.SessionKey == session.SessionKey).ToListAsync();
            var pits = await _db.PitStopEntries.Where(p => p.SessionKey == session.SessionKey).ToListAsync();
            var weather = await _db.WeatherReadings.Where(w => w.SessionKey == session.SessionKey).ToListAsync();

            if (grid.Count == 0 || results.Count == 0)
                continue;

            var polePace = grid.Where(g => g.LapDuration.HasValue).Select(g => g.LapDuration!.Value).DefaultIfEmpty(0).Min();
            var rainedInSession = weather.Any(w => w.Rainfall > 0);

            foreach (var result in results.Where(r => !r.Dns && r.Position.HasValue))
            {
                var gridEntry = grid.FirstOrDefault(g => g.DriverNumber == result.DriverNumber);
                var gridPosition = gridEntry?.Position ?? grid.Count + 1;

                var qualiGapToPole = gridEntry?.LapDuration.HasValue == true
                    ? gridEntry.LapDuration!.Value - polePace
                    : MissingLapTimeSentinel;

                var driverPits = pits.Where(p => p.DriverNumber == result.DriverNumber).ToList();
                var pitStopCount = driverPits.Count;
                var avgPitStopDuration = driverPits.Count == 0
                    ? 0
                    : driverPits.Average(p => p.StopDuration ?? p.LaneDuration ?? 0);

                _db.DriverRaceFeatures.Add(new DriverRaceFeature
                {
                    SessionKey = session.SessionKey,
                    DriverNumber = result.DriverNumber,
                    GridPosition = gridPosition,
                    QualiGapToPole = (float)qualiGapToPole,
                    PitStopCount = pitStopCount,
                    AvgPitStopDuration = (float)avgPitStopDuration,
                    Rainfall = rainedInSession ? 1 : 0,
                    FinishPosition = result.Position!.Value,
                    Podium = result.Position.Value <= 3 && !result.Dsq,
                    PointsFinish = result.Position.Value <= 10 && !result.Dsq
                });
            }
        }

        await _db.SaveChangesAsync();
    }
}
