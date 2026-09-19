using System.ComponentModel;
using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Analysis.ExplainDriverPrediction;
using F1Predictor.Application.Features.Analysis.ExplainRacePrediction;
using F1Predictor.Application.Features.Championship.GetForecast;
using F1Predictor.Application.Features.Championship.GetScenarios;
using F1Predictor.Application.Features.Championship.GetStandings;
using F1Predictor.Application.Features.Predictions.PredictRace;
using F1Predictor.Application.Features.Predictions.PreviewNextRace;
using F1Predictor.Application.Features.Seasons.GetRaces;
using Microsoft.Extensions.AI;
using SharedKernel;

namespace F1Predictor.Application.Features.Analysis;

/// <summary>
/// The analyst's tools: thin <see cref="AIFunction"/>s over the existing query handlers. Each
/// returns a small, rounded DTO with acronyms rather than names so that an 8B model's context
/// survives a three-tool question. Failures come back as a message, never an exception, so the
/// model can say "the model doesn't track that" instead of the request dying. The season is
/// bound from the command, not chosen by the model.
/// </summary>
internal sealed class AnalystTools(
    IQueryHandler<PreviewNextRaceQuery, NextRacePreviewResponse> preview,
    IQueryHandler<ExplainDriverPredictionQuery, DriverExplanationResponse> explain,
    IQueryHandler<GetStandingsQuery, StandingsResponse> standings,
    IQueryHandler<GetChampionshipForecastQuery, ChampionshipForecastResponse> forecast,
    IQueryHandler<GetTitleScenariosQuery, TitleScenariosResponse> scenarios,
    IQueryHandler<GetSeasonRacesQuery, IReadOnlyList<SeasonRaceResponse>> races,
    IQueryHandler<PredictRaceQuery, RacePredictionsResponse> predictions,
    IQueryHandler<ExplainRacePredictionQuery, DriverExplanationResponse> explainRace)
{
    public IList<AITool> For(int year) =>
    [
        AIFunctionFactory.Create((CancellationToken ct) => NextRacePreviewAsync(year, ct), "get_next_race_preview",
            "Podium and points probabilities for every driver in the next Grand Prix, and whether the grid is real or projected."),
        AIFunctionFactory.Create(([Description("Car number")] int driverNumber, CancellationToken ct) => ExplainDriverAsync(year, driverNumber, ct), "explain_driver",
            "Why the model rates one driver as it does for the next Grand Prix: each feature's contribution."),
        AIFunctionFactory.Create((CancellationToken ct) => StandingsAsync(year, ct), "get_standings",
            "Current drivers' (top 10) and constructors' championship tables."),
        AIFunctionFactory.Create((CancellationToken ct) => ForecastAsync(year, ct), "get_championship_forecast",
            "Title odds from the Monte Carlo season simulation, top 8 drivers, with the method caveat."),
        AIFunctionFactory.Create(([Description("How many contenders, default 5")] int topN = 5, CancellationToken ct = default) => ScenariosAsync(year, topN, ct), "get_title_scenarios",
            "What each leading contender needs to win the drivers' title."),
        AIFunctionFactory.Create((CancellationToken ct) => RacesAsync(year, ct), "get_season_races",
            "The season calendar with session keys, and which races are classified or sprints."),
        AIFunctionFactory.Create(([Description("Race session key from get_season_races")] int sessionKey, CancellationToken ct) => RacePredictionsAsync(sessionKey, ct), "get_race_predictions",
            "For a classified race: each driver's predicted probabilities against where they actually finished."),
        AIFunctionFactory.Create(([Description("Race session key from get_season_races")] int sessionKey, [Description("Car number")] int driverNumber, CancellationToken ct) => ExplainRaceDriverAsync(sessionKey, driverNumber, ct), "explain_race_driver",
            "Why the model rated one driver the way it did for a classified race, and how they actually finished: each feature's contribution.")
    ];

    public static string StatusFor(string functionName) => functionName switch
    {
        "get_next_race_preview" => "Looking at the next race preview…",
        "explain_driver" => "Reading the model's explanation…",
        "get_standings" => "Looking up championship standings…",
        "get_championship_forecast" => "Running the title odds…",
        "get_title_scenarios" => "Working out title scenarios…",
        "get_season_races" => "Checking the calendar…",
        "get_race_predictions" => "Comparing predictions with results…",
        "explain_race_driver" => "Comparing that driver's prediction with the result…",
        _ => "Consulting the model…"
    };

    private async Task<object> NextRacePreviewAsync(int year, CancellationToken ct)
    {
        var result = await preview.Handle(new PreviewNextRaceQuery(year), ct);
        if (result.IsFailure) return Unavailable(result.Error);
        var r = result.Value;
        return new
        {
            race = r.MeetingName, dateUtc = r.DateStart, gridConfirmed = r.GridConfirmed,
            drivers = r.Drivers.Select(d => new { acronym = d.NameAcronym, team = d.TeamName, grid = (int)d.GridPosition, podium = R(d.PodiumProbability), points = R(d.PointsProbability) })
        };
    }

    private async Task<object> ExplainDriverAsync(int year, int driverNumber, CancellationToken ct)
    {
        var result = await explain.Handle(new ExplainDriverPredictionQuery(year, driverNumber, IncludeNarrative: false), ct);
        if (result.IsFailure) return Unavailable(result.Error);
        var r = result.Value;
        return new
        {
            driver = r.NameAcronym,
            gridConfirmed = r.GridConfirmed,
            podium = TargetSummary(r.Podium),
            pointsFinish = TargetSummary(r.PointsFinish)
        };
    }

    private async Task<object> ExplainRaceDriverAsync(int sessionKey, int driverNumber, CancellationToken ct)
    {
        var result = await explainRace.Handle(new ExplainRacePredictionQuery(sessionKey, driverNumber), ct);
        if (result.IsFailure) return Unavailable(result.Error);
        var r = result.Value;
        return new
        {
            driver = r.NameAcronym,
            podium = TargetSummary(r.Podium),
            pointsFinish = TargetSummary(r.PointsFinish),
            finishPosition = r.FinishPosition,
            actualPodium = r.ActualPodium,
            actualPointsFinish = r.ActualPointsFinish
        };
    }

    private async Task<object> StandingsAsync(int year, CancellationToken ct)
    {
        var result = await standings.Handle(new GetStandingsQuery(year), ct);
        if (result.IsFailure) return Unavailable(result.Error);
        var r = result.Value;
        return new
        {
            drivers = r.Drivers.Take(10).Select(d => new { acronym = d.NameAcronym, points = R(d.Points), wins = d.Wins }),
            constructors = r.Constructors.Select(c => new { team = c.TeamName, points = R(c.Points), wins = c.Wins })
        };
    }

    private async Task<object> ForecastAsync(int year, CancellationToken ct)
    {
        var result = await forecast.Handle(new GetChampionshipForecastQuery(year), ct);
        if (result.IsFailure) return Unavailable(result.Error);
        var r = result.Value;
        return new
        {
            racesRemaining = r.RacesRemaining,
            sprintsRemaining = r.SprintsRemaining,
            method = r.Method,
            drivers = r.Drivers.Take(8).Select(d => new { acronym = d.NameAcronym, points = R(d.Points), titleProbability = R(d.TitleProbability), alive = d.IsMathematicallyAlive })
        };
    }

    private async Task<object> ScenariosAsync(int year, int topN, CancellationToken ct)
    {
        var clampedTopN = Math.Clamp(topN, 1, 10);
        var result = await scenarios.Handle(new GetTitleScenariosQuery(year, clampedTopN), ct);
        if (result.IsFailure) return Unavailable(result.Error);
        var r = result.Value;
        return new
        {
            leader = r.LeaderName,
            contenders = r.Contenders.Select(c => new { acronym = c.NameAcronym, points = R(c.Points), behind = R(c.PointsBehindLeader), alive = c.IsMathematicallyAlive, requirement = c.Requirement })
        };
    }

    private async Task<object> RacesAsync(int year, CancellationToken ct)
    {
        var result = await races.Handle(new GetSeasonRacesQuery(year), ct);
        if (result.IsFailure) return Unavailable(result.Error);
        return result.Value.Select(r => new { sessionKey = r.SessionKey, name = r.MeetingName, dateUtc = r.DateStart, isSprint = r.IsSprint, isClassified = r.IsClassified });
    }

    private async Task<object> RacePredictionsAsync(int sessionKey, CancellationToken ct)
    {
        var result = await predictions.Handle(new PredictRaceQuery(sessionKey), ct);
        if (result.IsFailure) return Unavailable(result.Error);
        var r = result.Value;
        return new
        {
            race = r.MeetingName,
            drivers = r.Drivers.Select(d => new
            {
                acronym = d.NameAcronym,
                grid = (int)d.GridPosition,
                finish = d.FinishPosition,
                podium = R(d.PodiumProbability),
                points = R(d.PointsProbability),
                actualPodium = d.ActualPodium
            })
        };
    }

    private static object TargetSummary(TargetExplanationResponse target) => new
    {
        probability = R(target.Probability),
        signals = target.Contributions.Select(c => new { feature = c.Feature, value = R(c.Value), contribution = R(c.Contribution), direction = c.Direction })
    };

    private static object Unavailable(Error error) => new { unavailable = true, reason = error.Description };
    private static float R(double value) => (float)Math.Round(value, 2);
}
