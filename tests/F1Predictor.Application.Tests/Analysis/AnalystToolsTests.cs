using System.Text.Json;
using F1Predictor.Application.Features.Analysis;
using F1Predictor.Application.Features.Analysis.ExplainDriverPrediction;
using F1Predictor.Application.Features.Analysis.ExplainRacePrediction;
using F1Predictor.Application.Features.Championship.GetForecast;
using F1Predictor.Application.Features.Championship.GetScenarios;
using F1Predictor.Application.Features.Championship.GetStandings;
using F1Predictor.Application.Features.Predictions.PredictRace;
using F1Predictor.Application.Features.Predictions.PreviewNextRace;
using F1Predictor.Application.Features.Seasons.GetRaces;
using F1Predictor.Application.Tests.Fakes;
using F1Predictor.Infrastructure.Database;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace F1Predictor.Application.Tests.Analysis;

public sealed class AnalystToolsTests
{
    private static AnalystTools ToolsWithRealExplain(ApplicationDbContext db, FakeChatClient chat) => new(
        new StubQueryHandler<PreviewNextRaceQuery, NextRacePreviewResponse>(),
        new ExplainDriverPredictionQueryHandler(db, new FakeRacePredictor(), new FakeAiCapabilities(chatAvailable: true), chat,
            new ServiceCollection().AddHybridCache().Services.BuildServiceProvider().GetRequiredService<HybridCache>(),
            NullLogger<ExplainDriverPredictionQueryHandler>.Instance),
        new StubQueryHandler<GetStandingsQuery, StandingsResponse>(),
        new StubQueryHandler<GetChampionshipForecastQuery, ChampionshipForecastResponse>(),
        new StubQueryHandler<GetTitleScenariosQuery, TitleScenariosResponse>(),
        new StubQueryHandler<GetSeasonRacesQuery, IReadOnlyList<SeasonRaceResponse>>(),
        new StubQueryHandler<PredictRaceQuery, RacePredictionsResponse>(),
        new StubQueryHandler<ExplainRacePredictionQuery, DriverExplanationResponse>());

    // The tool returns the contribution table only, so it must not spend a second LLM
    // generation on a narrative it then discards — inside a chat the provider is by
    // definition available, which is exactly when the handler would otherwise write one.
    [Fact]
    public async Task ExplainDriver_InvokedAsTool_ReturnsContributionsWithoutCallingChatClient()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: true);
        var chat = new FakeChatClient();
        var explain = (AIFunction)ToolsWithRealExplain(db, chat).For(2026).Single(t => t.Name == "explain_driver");

        var result = await explain.InvokeAsync(new AIFunctionArguments { ["driverNumber"] = 3 }, CancellationToken.None);

        chat.Calls.Should().BeEmpty();
        var json = JsonSerializer.Serialize(result);
        json.Should().Contain("\"driver\":\"D03\"");
        json.Should().Contain("\"signals\"");
        json.Should().NotContain("narrative");
        json.Should().NotContain("unavailable");
    }

    // The description promises "default 5"; the schema has to agree, or the model is told a
    // parameter is optional that the runtime then refuses to call without.
    [Fact]
    public void TitleScenarios_Schema_MakesTopNOptional()
    {
        using var db = InMemoryDb.Create();
        var scenarios = (AIFunction)ToolsWithRealExplain(db, new FakeChatClient()).For(2026).Single(t => t.Name == "get_title_scenarios");

        var schema = scenarios.JsonSchema;

        schema.GetProperty("properties").TryGetProperty("topN", out _).Should().BeTrue();
        var required = schema.TryGetProperty("required", out var r) ? r.EnumerateArray().Select(e => e.GetString()).ToList() : [];
        required.Should().NotContain("topN");
    }
}
