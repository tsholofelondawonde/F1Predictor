namespace F1Predictor.Domain.RaceData.Entities;

/// <summary>
/// A single pit stop. <see cref="StopDuration"/> is null for races before the 2024 US GP,
/// where <see cref="LaneDuration"/> is the only figure available.
/// </summary>
public class PitStopEntry
{
    public int Id { get; set; }
    public int SessionKey { get; set; }
    public int DriverNumber { get; set; }
    public double? StopDuration { get; set; }
    public double? LaneDuration { get; set; }

    /// <summary>The best available duration for this stop, preferring the stationary time.</summary>
    public double EffectiveDuration => StopDuration ?? LaneDuration ?? 0;
}
