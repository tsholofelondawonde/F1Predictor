namespace F1Predictor.Domain.RaceData.Entities;

/// <summary>
/// A point-in-time weather sample taken during a race session.
/// </summary>
public class WeatherReading
{
    public int Id { get; set; }
    public int SessionKey { get; set; }

    /// <summary>OpenF1 reports this as 0 or 1 rather than a measured amount.</summary>
    public double Rainfall { get; set; }

    public double TrackTemperature { get; set; }
}
