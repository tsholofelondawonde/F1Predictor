namespace F1Predictor.Domain.RaceData.Entities;

/// <summary>
/// One driver's classified result for a race session.
/// </summary>
public class SessionResultEntry
{
    public int Id { get; set; }
    public int SessionKey { get; set; }
    public int DriverNumber { get; set; }

    /// <summary>Finishing position. Null when the driver was not classified.</summary>
    public int? Position { get; set; }

    /// <summary>
    /// Championship points awarded, straight from OpenF1. Taking the number rather than
    /// deriving it from <see cref="Position"/> means the sprint scale and any fastest-lap
    /// rule come for free, and a rule change never needs a code change here.
    /// </summary>
    public double Points { get; set; }

    public bool Dnf { get; set; }
    public bool Dns { get; set; }
    public bool Dsq { get; set; }
}
