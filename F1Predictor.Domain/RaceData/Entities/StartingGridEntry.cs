namespace F1Predictor.Domain.RaceData.Entities;

/// <summary>
/// One driver's grid slot for a race, as returned by OpenF1's <c>starting_grid</c> endpoint.
/// </summary>
public class StartingGridEntry
{
    public int Id { get; set; }

    /// <summary>The <em>race</em> session this grid belongs to, not the qualifying session it was fetched from.</summary>
    public int SessionKey { get; set; }

    public int DriverNumber { get; set; }
    public int? Position { get; set; }

    /// <summary>Qualifying lap time in seconds. Null when the driver set no time.</summary>
    public double? LapDuration { get; set; }
}
