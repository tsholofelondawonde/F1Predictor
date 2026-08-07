namespace F1Predictor.Data;

public class Meeting
{
    public int MeetingKey { get; set; }
    public int Year { get; set; }
    public string CircuitShortName { get; set; } = "";
    public string CountryName { get; set; } = "";
    public string MeetingName { get; set; } = "";
    public DateTimeOffset DateStart { get; set; }
}

public class RaceSession
{
    public int SessionKey { get; set; }
    public int MeetingKey { get; set; }
    public string SessionName { get; set; } = "";
    public string SessionType { get; set; } = "";
    public DateTimeOffset DateStart { get; set; }

    // Not part of the CLAUDE.md spec's raw session_key set: the OpenF1 starting_grid
    // endpoint is keyed by the qualifying session, not the race session (verified live),
    // so the race row needs to carry a pointer to its own qualifying session.
    public int? QualifyingSessionKey { get; set; }
}

public class StartingGridEntry
{
    public int Id { get; set; }
    public int SessionKey { get; set; }
    public int DriverNumber { get; set; }
    public int? Position { get; set; }
    public double? LapDuration { get; set; }
}

public class SessionResultEntry
{
    public int Id { get; set; }
    public int SessionKey { get; set; }
    public int DriverNumber { get; set; }
    public int? Position { get; set; }
    public bool Dnf { get; set; }
    public bool Dns { get; set; }
    public bool Dsq { get; set; }
}

public class PitStopEntry
{
    public int Id { get; set; }
    public int SessionKey { get; set; }
    public int DriverNumber { get; set; }
    public double? StopDuration { get; set; }
    public double? LaneDuration { get; set; }
}

public class WeatherReading
{
    public int Id { get; set; }
    public int SessionKey { get; set; }
    public double Rainfall { get; set; }
    public double TrackTemperature { get; set; }
}

public class DriverRaceFeature
{
    public int Id { get; set; }
    public int SessionKey { get; set; }
    public int DriverNumber { get; set; }
    public float GridPosition { get; set; }
    public float QualiGapToPole { get; set; }
    public float PitStopCount { get; set; }
    public float AvgPitStopDuration { get; set; }
    public float Rainfall { get; set; }
    public int FinishPosition { get; set; }
    public bool Podium { get; set; }
    public bool PointsFinish { get; set; }
}
