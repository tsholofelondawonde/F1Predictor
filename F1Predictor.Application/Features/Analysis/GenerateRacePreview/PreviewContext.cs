using System.Text.Json;

namespace F1Predictor.Application.Features.Analysis.GenerateRacePreview;

/// <summary>
/// The compact, rounded slice of model output a preview is written from. Small on purpose: an
/// 8B model's context has to hold this plus the instructions plus the answer.
/// </summary>
internal sealed record PreviewContext(PreviewRace Race, IReadOnlyList<PreviewDriverRow> Drivers, IReadOnlyList<PreviewStandingRow> Standings, IReadOnlyList<PreviewOddsRow> TitleOdds)
{
    private const int DriversToInclude = 8;
    private const int SignalsPerDriver = 2;
    private const int StandingsToInclude = 5;
    private const int OddsToInclude = 3;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static PreviewContext Build(PreviewRace race, IReadOnlyList<PreviewDriverSignal> drivers, IReadOnlyList<PreviewStandingRow> standings, IReadOnlyList<PreviewOddsRow> titleOdds) => new(
        race,
        [.. drivers.OrderByDescending(d => d.PodiumProbability).Take(DriversToInclude).Select(d => new PreviewDriverRow(
            d.Acronym, d.Team, d.GridPosition, Round(d.PodiumProbability), Round(d.PointsProbability),
            [.. d.Contributions.OrderByDescending(c => Math.Abs(c.Contribution)).Take(SignalsPerDriver).Select(c => c with { Contribution = Round(c.Contribution) })]))],
        [.. standings.Take(StandingsToInclude)],
        [.. titleOdds.Take(OddsToInclude).Select(o => o with { Probability = Round(o.Probability) })]);

    public string ToPromptJson() => JsonSerializer.Serialize(this, Json);

    private static float Round(float value) => MathF.Round(value, 2);
}

internal sealed record PreviewRace(string Name, string Circuit, string Country, DateTimeOffset DateUtc, bool GridConfirmed);
internal sealed record PreviewDriverSignal(string Acronym, string Team, int GridPosition, float PodiumProbability, float PointsProbability, IReadOnlyList<PreviewSignal> Contributions);
internal sealed record PreviewSignal(string Feature, string Direction, float Contribution);
internal sealed record PreviewDriverRow(string Acronym, string Team, int Grid, float Podium, float Points, IReadOnlyList<PreviewSignal> Signals);
internal sealed record PreviewStandingRow(string Acronym, double Points, int Wins);
internal sealed record PreviewOddsRow(string Acronym, float Probability);
