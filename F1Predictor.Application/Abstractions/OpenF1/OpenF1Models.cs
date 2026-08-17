namespace F1Predictor.Application.Abstractions.OpenF1;

/// <summary>
/// Shapes returned by the OpenF1 API. Property names are the PascalCase form of the
/// wire's snake_case fields; the Infrastructure client applies a snake-case naming policy
/// rather than annotating every property, which keeps these free of serialisation concerns.
/// </summary>
public sealed record OpenF1Meeting
{
    public int MeetingKey { get; init; }
    public int Year { get; init; }
    public string CircuitShortName { get; init; } = "";
    public string CountryName { get; init; } = "";
    public string MeetingName { get; init; } = "";
    public DateTimeOffset DateStart { get; init; }
}

public sealed record OpenF1Session
{
    public int SessionKey { get; init; }
    public int MeetingKey { get; init; }
    public string SessionName { get; init; } = "";
    public string SessionType { get; init; } = "";
    public DateTimeOffset DateStart { get; init; }
}

public sealed record OpenF1StartingGrid
{
    public int? Position { get; init; }
    public int DriverNumber { get; init; }
    public double? LapDuration { get; init; }
}

public sealed record OpenF1SessionResult
{
    public int DriverNumber { get; init; }
    public int? Position { get; init; }

    /// <summary>
    /// Championship points awarded for this result. OpenF1 applies the correct scale itself,
    /// including the sprint's 8-7-6-5-4-3-2-1, so no points table is needed on our side.
    /// </summary>
    public double? Points { get; init; }

    public bool Dnf { get; init; }
    public bool Dns { get; init; }
    public bool Dsq { get; init; }
}

public sealed record OpenF1Driver
{
    public int DriverNumber { get; init; }
    public string FullName { get; init; } = "";
    public string NameAcronym { get; init; } = "";
    public string TeamName { get; init; } = "";
    public string TeamColour { get; init; } = "";
}

public sealed record OpenF1Pit
{
    public int DriverNumber { get; init; }
    public double? StopDuration { get; init; }
    public double? LaneDuration { get; init; }
}

public sealed record OpenF1Weather
{
    public double Rainfall { get; init; }
    public double TrackTemperature { get; init; }
}
