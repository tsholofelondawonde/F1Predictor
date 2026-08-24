namespace F1Predictor.Domain.RaceData.Entities;

/// <summary>
/// A race weekend, keyed by OpenF1's natural <c>meeting_key</c>.
/// </summary>
public class Meeting
{
    public int MeetingKey { get; set; }
    public int Year { get; set; }
    public string CircuitShortName { get; set; } = "";
    public string CountryName { get; set; } = "";
    public string MeetingName { get; set; } = "";
    public DateTimeOffset DateStart { get; set; }
}
