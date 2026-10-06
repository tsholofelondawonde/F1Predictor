using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using F1Predictor.Domain.RaceData.Entities;

namespace F1Predictor.Domain.Analysis;

/// <summary>
/// A deterministic, human-readable summary of one classified race: the same story a fan would
/// tell, rendered from the same raw rows the rest of the domain already ingests.
/// </summary>
/// <remarks>
/// <see cref="Build"/> is pure — same inputs, same <see cref="Text"/>, every time. That
/// determinism is what makes <see cref="ContentHash"/> (SHA-256 of <see cref="Text"/>) a cheap
/// "has this race's story changed" check: an embedding index can skip re-embedding a race whose
/// hash hasn't moved, and a hash mismatch is exactly the signal that should trigger a re-embed.
/// The individual fields (<see cref="Winner"/>, <see cref="Podium"/>, ...) are the structured
/// facts <see cref="Text"/> is rendered from; callers that only need one fact (e.g. an analyst
/// tool) can read it directly instead of parsing the prose.
/// </remarks>
public sealed record RaceFactSheet(
    string Winner,
    IReadOnlyList<string> Podium,
    string PoleSitter,
    bool Rained,
    string BiggestGainer,
    string BiggestLoser,
    int Retirements,
    double AveragePitStops,
    string FastestStop,
    IReadOnlyDictionary<string, int> PointsScorersByTeam,
    string Text,
    string ContentHash)
{
    /// <summary>Used when a driver has no entry in the directory — should not happen in practice
    /// (every session that has results also has an entry list), but keeps <see cref="Build"/>
    /// total rather than throwing on a data gap.</summary>
    private const string UnknownDriverAcronym = "UNK";
    private const string UnknownTeamName = "Unknown Team";

    /// <summary>
    /// Builds the fact sheet for one classified race. Callers pass the same rows
    /// <see cref="F1Predictor.Domain.Predictions.DriverRaceFeature"/> is built from, plus the
    /// session's driver directory for names and teams.
    /// </summary>
    /// <param name="meeting">The race weekend.</param>
    /// <param name="session">The race session itself — its own <see cref="RaceSession.DateStart"/>
    /// is used for the date, since that is the day the race actually ran, not the first day of
    /// the weekend.</param>
    /// <param name="results">This session's results, including non-starters and retirements.</param>
    /// <param name="grid">This session's starting grid (from qualifying — see the ml-pipeline
    /// note on <c>QualifyingSessionKey</c>). A driver missing from this list is treated as a pit
    /// lane start: their grid position becomes <c>grid.Count + 1</c>, the same fallback
    /// <see cref="F1Predictor.Domain.Predictions.DriverRaceFeature.Create"/> uses.</param>
    /// <param name="pitStops">This session's pit stops, across all drivers.</param>
    /// <param name="weather">This session's weather samples.</param>
    /// <param name="directory">Driver number to <see cref="DriverEntry"/>, for names and teams.</param>
    public static RaceFactSheet Build(
        Meeting meeting,
        RaceSession session,
        IReadOnlyList<SessionResultEntry> results,
        IReadOnlyList<StartingGridEntry> grid,
        IReadOnlyList<PitStopEntry> pitStops,
        IReadOnlyList<WeatherReading> weather,
        IReadOnlyDictionary<int, DriverEntry> directory)
    {
        ArgumentNullException.ThrowIfNull(meeting);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(pitStops);
        ArgumentNullException.ThrowIfNull(weather);
        ArgumentNullException.ThrowIfNull(directory);

        var lookup = new DriverLookup(directory, grid);

        // "Started" excludes only DNS — a DNF driver still lined up and still pitted before
        // retiring, so they count towards the field for the average pit-stop figure.
        var started = results.Where(r => !r.Dns).ToList();

        // Classified for movement purposes: they have a finishing position, whether or not a
        // post-race penalty later disqualified them (a DSQ still drove the race and can still
        // gain or lose places on track).
        var classified = results
            .Where(r => !r.Dns && r.Position.HasValue)
            .OrderBy(r => r.Position!.Value)
            .ToList();

        // Official classification for winner/podium excludes a DSQ — their raw finishing
        // position does not count as a result once disqualified.
        var officiallyClassified = classified.Where(r => !r.Dsq).ToList();

        var (winner, winnerTeam, winnerGrid, podium) = DetermineResult(officiallyClassified, lookup);

        var poleEntry = grid.FirstOrDefault(g => g.Position == 1);
        var poleSitter = poleEntry is null ? UnknownDriverAcronym : lookup.AcronymOf(poleEntry.DriverNumber);

        var rained = weather.Any(w => w.Rainfall > 0);

        var (biggestGainer, biggestLoser) = DetermineMovement(classified, lookup);

        var retirements = results.Count(r => r.Dnf);

        var averagePitStops = started.Count == 0 ? 0 : (double)pitStops.Count / started.Count;

        var fastestStop = DetermineFastestStop(pitStops, lookup);

        var pointsScorersByTeam = DeterminePointsScorersByTeam(officiallyClassified, lookup);

        var text = BuildText(
            meeting, session, winner, winnerTeam, winnerGrid, podium, poleSitter, rained,
            biggestGainer, biggestLoser, retirements, averagePitStops, fastestStop, pointsScorersByTeam);

        var contentHash = ComputeHash(text);

        return new RaceFactSheet(
            winner, podium, poleSitter, rained, biggestGainer, biggestLoser, retirements,
            averagePitStops, fastestStop, pointsScorersByTeam, text, contentHash);
    }

    private static (string Winner, string WinnerTeam, int WinnerGrid, List<string> Podium) DetermineResult(
        IReadOnlyList<SessionResultEntry> officiallyClassified, DriverLookup lookup)
    {
        var winnerResult = officiallyClassified.FirstOrDefault(r => r.Position == 1);
        var winner = winnerResult is null ? UnknownDriverAcronym : lookup.AcronymOf(winnerResult.DriverNumber);
        var winnerTeam = winnerResult is null ? UnknownTeamName : lookup.TeamOf(winnerResult.DriverNumber);
        var winnerGrid = winnerResult is null ? 0 : lookup.GridPositionOf(winnerResult);
        var podium = officiallyClassified.Take(3).Select(r => lookup.AcronymOf(r.DriverNumber)).ToList();

        return (winner, winnerTeam, winnerGrid, podium);
    }

    /// <summary>
    /// Gain = grid position minus finish position; positive means the driver moved forward. Ties
    /// (equal gain) break on the lower driver number, so the choice never depends on enumeration
    /// order.
    /// </summary>
    private static (string Gainer, string Loser) DetermineMovement(
        IReadOnlyList<SessionResultEntry> classified, DriverLookup lookup)
    {
        var gains = classified
            .Select(r => (Result: r, Gain: lookup.GridPositionOf(r) - r.Position!.Value))
            .ToList();

        // A session flagged classified can still carry no classified results — OpenF1 has rounds
        // it never published a result for — and that must not fail the whole fact sheet.
        if (gains.Count == 0)
        {
            return ("n/a", "n/a");
        }

        var gainer = gains.OrderByDescending(g => g.Gain).ThenBy(g => g.Result.DriverNumber).First();
        var loser = gains.OrderBy(g => g.Gain).ThenBy(g => g.Result.DriverNumber).First();

        return (
            FormatMove(gainer.Result, lookup.GridPositionOf(gainer.Result), lookup.AcronymOf),
            FormatMove(loser.Result, lookup.GridPositionOf(loser.Result), lookup.AcronymOf));
    }

    private static string DetermineFastestStop(IReadOnlyList<PitStopEntry> pitStops, DriverLookup lookup)
    {
        var fastest = pitStops.OrderBy(p => p.EffectiveDuration).ThenBy(p => p.DriverNumber).FirstOrDefault();

        return fastest is null
            ? "n/a"
            : string.Create(CultureInfo.InvariantCulture, $"{fastest.EffectiveDuration:0.00}s ({lookup.AcronymOf(fastest.DriverNumber)})");
    }

    private static Dictionary<string, int> DeterminePointsScorersByTeam(
        IReadOnlyList<SessionResultEntry> officiallyClassified, DriverLookup lookup) =>
        officiallyClassified
            .Where(r => r.Points > 0)
            .GroupBy(r => lookup.TeamOf(r.DriverNumber))
            .ToDictionary(g => g.Key, g => g.Count());

    private static string FormatMove(SessionResultEntry result, int gridPosition, Func<int, string> acronymOf) =>
        $"{acronymOf(result.DriverNumber)} P{gridPosition.ToString(CultureInfo.InvariantCulture)}→P{result.Position!.Value.ToString(CultureInfo.InvariantCulture)}";

    private static string BuildText(
        Meeting meeting,
        RaceSession session,
        string winner,
        string winnerTeam,
        int winnerGrid,
        IReadOnlyList<string> podium,
        string poleSitter,
        bool rained,
        string biggestGainer,
        string biggestLoser,
        int retirements,
        double averagePitStops,
        string fastestStop,
        IReadOnlyDictionary<string, int> pointsScorersByTeam)
    {
        var weatherWord = rained ? "Wet" : "Dry";
        var podiumText = string.Join(", ", podium);
        var retirementWord = retirements == 1 ? "retirement" : "retirements";

        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"{meeting.Year} {meeting.MeetingName}, {meeting.CountryName}, {session.DateStart:d MMMM yyyy}. ");
        sb.Append(weatherWord).Append(". ");
        sb.Append("Pole ").Append(poleSitter).Append(". ");
        sb.Append("Winner ").Append(winner).Append(" (").Append(winnerTeam).Append(") from P")
            .Append(winnerGrid.ToString(CultureInfo.InvariantCulture)).Append("; podium ").Append(podiumText).Append(". ");
        sb.Append("Biggest gainer ").Append(biggestGainer).Append(", biggest loser ").Append(biggestLoser).Append(". ");
        sb.Append(retirements.ToString(CultureInfo.InvariantCulture)).Append(' ').Append(retirementWord).Append(". ");
        sb.Append(CultureInfo.InvariantCulture, $"Average {averagePitStops:0.0} pit stops, fastest stop {fastestStop}.");

        AppendPointsScorers(sb, pointsScorersByTeam);

        return sb.ToString();
    }

    private static void AppendPointsScorers(StringBuilder sb, IReadOnlyDictionary<string, int> pointsScorersByTeam)
    {
        sb.Append("\n\nPoints scorers by team:");

        foreach (var (team, count) in pointsScorersByTeam
                     .OrderByDescending(kvp => kvp.Value)
                     .ThenBy(kvp => kvp.Key, StringComparer.Ordinal)
                     .Select(kvp => (kvp.Key, kvp.Value)))
        {
            sb.Append("\n- ").Append(team).Append(": ").Append(count.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static string ComputeHash(string text) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    /// <summary>Resolves a driver number to its acronym, team and effective grid position for one
    /// session — bundled together so <see cref="Build"/>'s helper methods don't each need four
    /// separate parameters.</summary>
    private sealed class DriverLookup
    {
        private readonly IReadOnlyDictionary<int, DriverEntry> _directory;
        private readonly Dictionary<int, int> _gridByDriver;
        private readonly int _gridCount;

        public DriverLookup(IReadOnlyDictionary<int, DriverEntry> directory, IReadOnlyList<StartingGridEntry> grid)
        {
            _directory = directory;
            _gridCount = grid.Count;
            _gridByDriver = grid
                .Where(g => g.Position.HasValue)
                .ToDictionary(g => g.DriverNumber, g => g.Position!.Value);
        }

        public string AcronymOf(int driverNumber) =>
            _directory.TryGetValue(driverNumber, out var entry) && !string.IsNullOrWhiteSpace(entry.NameAcronym)
                ? entry.NameAcronym
                : UnknownDriverAcronym;

        public string TeamOf(int driverNumber) =>
            _directory.TryGetValue(driverNumber, out var entry) && !string.IsNullOrWhiteSpace(entry.TeamName)
                ? entry.TeamName
                : UnknownTeamName;

        /// <summary>No grid entry means a pit lane start or similar: treat them as starting behind
        /// the whole field rather than dropping them from the gainer/loser comparison.</summary>
        public int GridPositionOf(SessionResultEntry result) =>
            _gridByDriver.TryGetValue(result.DriverNumber, out var position) ? position : _gridCount + 1;
    }
}
