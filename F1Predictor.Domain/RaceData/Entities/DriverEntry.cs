namespace F1Predictor.Domain.RaceData.Entities;

/// <summary>
/// One driver's entry for a session: who they are and who they drove for.
/// </summary>
/// <remarks>
/// Held per session rather than per season because that is how OpenF1 keys it, and because
/// it makes mid-season driver and team changes fall out for free rather than needing a
/// validity window. It is also the only source of a driver's name anywhere in the system —
/// everything else identifies a driver by their car number alone.
/// <para>
/// OpenF1 populates this for sessions that have not run yet, which is what lets the next
/// race be previewed with a real entry list before anyone has driven.
/// </para>
/// </remarks>
public class DriverEntry
{
    public int Id { get; set; }
    public int SessionKey { get; set; }
    public int DriverNumber { get; set; }

    /// <summary>Full name, e.g. "Max VERSTAPPEN".</summary>
    public string FullName { get; set; } = "";

    /// <summary>Three-letter abbreviation, e.g. "VER".</summary>
    public string NameAcronym { get; set; } = "";

    /// <summary>Constructor name, e.g. "Red Bull Racing".</summary>
    public string TeamName { get; set; } = "";

    /// <summary>Team colour as a six-digit hex string with no leading hash, e.g. "4781D7".</summary>
    public string TeamColour { get; set; } = "";
}
