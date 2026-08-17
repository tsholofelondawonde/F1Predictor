using SharedKernel;

namespace F1Predictor.Application.Features.Championship;

internal static class ChampionshipErrors
{
    public static Error NoResults(int year) => Error.NotFound(
        "Championship.NoResults",
        $"No classified race results exist for {year}.",
        "There are no results for that season yet. Ingest the season first.");

    public static Error SeasonComplete(int year) => Error.Problem(
        "Championship.SeasonComplete",
        $"Every session of {year} has been run, so there is nothing left to forecast.",
        "That season is over — the championship is already decided.");
}
