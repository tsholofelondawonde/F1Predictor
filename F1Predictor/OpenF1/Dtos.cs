using System.Text.Json.Serialization;

namespace F1Predictor.OpenF1;

public record MeetingDto
{
    [JsonPropertyName("meeting_key")]
    public int MeetingKey { get; init; }

    [JsonPropertyName("year")]
    public int Year { get; init; }

    [JsonPropertyName("circuit_short_name")]
    public string CircuitShortName { get; init; } = "";

    [JsonPropertyName("country_name")]
    public string CountryName { get; init; } = "";

    [JsonPropertyName("meeting_name")]
    public string MeetingName { get; init; } = "";

    [JsonPropertyName("date_start")]
    public DateTimeOffset DateStart { get; init; }
}

public record SessionDto
{
    [JsonPropertyName("session_key")]
    public int SessionKey { get; init; }

    [JsonPropertyName("meeting_key")]
    public int MeetingKey { get; init; }

    [JsonPropertyName("session_name")]
    public string SessionName { get; init; } = "";

    [JsonPropertyName("session_type")]
    public string SessionType { get; init; } = "";

    [JsonPropertyName("date_start")]
    public DateTimeOffset DateStart { get; init; }
}

public record StartingGridDto
{
    [JsonPropertyName("position")]
    public int? Position { get; init; }

    [JsonPropertyName("driver_number")]
    public int DriverNumber { get; init; }

    [JsonPropertyName("lap_duration")]
    public double? LapDuration { get; init; }
}

public record SessionResultDto
{
    [JsonPropertyName("driver_number")]
    public int DriverNumber { get; init; }

    [JsonPropertyName("position")]
    public int? Position { get; init; }

    [JsonPropertyName("dnf")]
    public bool Dnf { get; init; }

    [JsonPropertyName("dns")]
    public bool Dns { get; init; }

    [JsonPropertyName("dsq")]
    public bool Dsq { get; init; }
}

public record PitDto
{
    [JsonPropertyName("driver_number")]
    public int DriverNumber { get; init; }

    [JsonPropertyName("stop_duration")]
    public double? StopDuration { get; init; }

    [JsonPropertyName("lane_duration")]
    public double? LaneDuration { get; init; }
}

public record WeatherDto
{
    [JsonPropertyName("rainfall")]
    public double Rainfall { get; init; }

    [JsonPropertyName("track_temperature")]
    public double TrackTemperature { get; init; }
}
