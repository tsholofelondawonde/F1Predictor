using F1Predictor.Application.Features.Analysis.GenerateRacePreview;
using FluentAssertions;
using Xunit;

namespace F1Predictor.Application.Tests.Analysis;

public sealed class PreviewContextTests
{
    private static PreviewDriverSignal Driver(int n, float podium) => new($"D{n:00}", "Team", n, podium, 0.5f,
        [new("GridPosition", "helps", 1.23456f), new("QualiGapToPole", "hurts", -0.7f), new("Rainfall", "neutral", 0f), new("PitStopCount", "hurts", -0.02f)]);

    [Fact]
    public void Build_TwentyDrivers_KeepsTopEightByPodiumProbability()
    {
        var drivers = Enumerable.Range(1, 20).Select(n => Driver(n, 1f - n * 0.04f)).ToList();

        var context = PreviewContext.Build(new PreviewRace("GP", "Circuit", "Country", DateTimeOffset.UtcNow, GridConfirmed: false), drivers, standings: [], titleOdds: []);

        context.Drivers.Should().HaveCount(8);
        context.Drivers[0].Acronym.Should().Be("D01");
    }

    [Fact]
    public void Build_Signals_KeepsTwoLargestByMagnitudeRoundedToTwoDecimals()
    {
        var context = PreviewContext.Build(new PreviewRace("GP", "C", "X", DateTimeOffset.UtcNow, true), [Driver(1, 0.9f)], [], []);

        context.Drivers[0].Signals.Should().HaveCount(2);
        context.Drivers[0].Signals[0].Feature.Should().Be("GridPosition");
        context.Drivers[0].Signals[0].Contribution.Should().Be(1.23f);
        context.Drivers[0].Signals[1].Feature.Should().Be("QualiGapToPole");
        context.Drivers[0].Podium.Should().Be(0.9f);
    }

    [Fact]
    public void ToPromptJson_IsCompactCamelCase()
    {
        var context = PreviewContext.Build(new PreviewRace("GP", "C", "X", DateTimeOffset.UtcNow, false), [Driver(1, 0.9f)], [], []);

        var json = context.ToPromptJson();

        json.Should().Contain("\"gridConfirmed\":false").And.NotContain("\n");
    }
}
