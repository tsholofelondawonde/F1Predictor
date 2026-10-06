using F1Predictor.Domain.Championship;
using F1Predictor.Domain.RaceData.Entities;

namespace F1Predictor.Application.Tests.Championship;

/// <summary>
/// A season in which cars 1 and 2 (one team) finish first and second in every race so far, and
/// cars 3 and 4 (the next team) third and fourth. Nobody has ever beaten them, which is exactly
/// the shape a Plackett–Luce fit has no finite answer for without a prior.
/// </summary>
internal static class DominantSeason
{
    public const int Drivers = 20;

    public static List<RaceOutcome> Outcomes(int races) =>
        [.. Enumerable.Range(1, races).Select(race => new RaceOutcome(race, FinishingOrder(race), []))];

    public static IReadOnlyList<int> FinishingOrder(int race)
    {
        // Cars 5..20 shuffle between races so no one of them is separated from the rest.
        var back = Enumerable.Range(5, Drivers - 4).ToList();
        var shift = race % back.Count;

        return [1, 2, 3, 4, .. back.Skip(shift), .. back.Take(shift)];
    }

    public static ChampionshipStandings Standings(int races)
    {
        var results = new List<SessionResultEntry>();

        for (var race = 1; race <= races; race++)
        {
            var order = FinishingOrder(race);

            for (var i = 0; i < order.Count; i++)
            {
                results.Add(new SessionResultEntry
                {
                    SessionKey = race,
                    DriverNumber = order[i],
                    Position = i + 1,
                    Points = ChampionshipPoints.ForRacePosition(i + 1),
                });
            }
        }

        var entries = Enumerable.Range(1, Drivers).ToDictionary(
            number => number,
            number => new DriverEntry
            {
                SessionKey = 1,
                DriverNumber = number,
                FullName = $"Driver {number}",
                NameAcronym = $"D{number:00}",
                TeamName = $"Team {(number + 1) / 2}",
                TeamColour = "FFFFFF",
            });

        return ChampionshipStandings.Build(results, new HashSet<int>(), entries);
    }

    public static List<RemainingSession> Remaining(int races, int firstKey) =>
        [.. Enumerable.Range(firstKey, races).Select(key => new RemainingSession(key, false))];
}
