using F1Predictor.Application.Features.Analysis.ExplainRacePrediction;
using F1Predictor.Application.Tests.Fakes;
using F1Predictor.Domain.RaceData.Entities;
using F1Predictor.Infrastructure.Database;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace F1Predictor.Application.Tests.Analysis;

public sealed class ExplainRacePredictionQueryHandlerTests
{
    private const int ClassifiedSessionKey = 10;

    private static ExplainRacePredictionQueryHandler Handler(ApplicationDbContext db, FakeChatClient chat, bool aiAvailable, bool modelsAvailable = true) =>
        new(db, new FakeRacePredictor { ModelsAvailable = modelsAvailable }, new FakeAiCapabilities(aiAvailable), chat,
            new ServiceCollection().AddHybridCache().Services.BuildServiceProvider().GetRequiredService<HybridCache>(),
            NullLogger<ExplainRacePredictionQueryHandler>.Instance);

    private static async Task SeedAsync(ApplicationDbContext db)
    {
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: false);
        // Session 10 (the already-classified "Past GP") only gets feature rows from SeasonSeed;
        // add the entries needed to describe a driver by name.
        db.DriverEntries.Add(new DriverEntry { SessionKey = 10, DriverNumber = 1, FullName = "Driver 1", NameAcronym = "D01", TeamName = "Team", TeamColour = "FF0000" });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_AiUnavailable_ReturnsStructuredExplanationWithOutcomeFields()
    {
        using var db = InMemoryDb.Create();
        await SeedAsync(db);
        var chat = new FakeChatClient();

        var result = await Handler(db, chat, aiAvailable: false).Handle(new(ClassifiedSessionKey, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Narrative.Should().BeNull();
        result.Value.Model.Should().BeNull();
        result.Value.Podium.Contributions.Should().HaveCount(5);
        result.Value.FinishPosition.Should().Be(1);
        result.Value.ActualPodium.Should().BeTrue();
        result.Value.ActualPointsFinish.Should().BeTrue();
        chat.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_AiAvailable_AddsNarrativeAndCachesIt()
    {
        using var db = InMemoryDb.Create();
        await SeedAsync(db);
        var chat = new FakeChatClient { ReplyText = "The grid-position contribution held up." };
        var handler = Handler(db, chat, aiAvailable: true);

        var first = await handler.Handle(new(ClassifiedSessionKey, 1), CancellationToken.None);
        var second = await handler.Handle(new(ClassifiedSessionKey, 1), CancellationToken.None);

        first.Value.Narrative.Should().Be("The grid-position contribution held up.");
        first.Value.Model.Should().Be("fake-model");
        second.Value.Narrative.Should().Be("The grid-position contribution held up.");
        chat.Calls.Should().HaveCount(1);
        chat.Calls[0][0].Role.Should().Be(ChatRole.System);
        chat.Calls[0][1].Text.Should().Contain("finishPosition");
    }

    [Fact]
    public async Task Handle_ChatClientFails_StillReturnsStructuredExplanation()
    {
        using var db = InMemoryDb.Create();
        await SeedAsync(db);
        var chat = new FakeChatClient { Throws = new HttpRequestException("ollama down") };

        var result = await Handler(db, chat, aiAvailable: true).Handle(new(ClassifiedSessionKey, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Narrative.Should().BeNull();
        result.Value.FinishPosition.Should().Be(1);
    }

    [Fact]
    public async Task Handle_DriverNotInRace_ReturnsDriverNotInRace()
    {
        using var db = InMemoryDb.Create();
        await SeedAsync(db);

        var result = await Handler(db, new FakeChatClient(), aiAvailable: false).Handle(new(ClassifiedSessionKey, 99), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Analysis.DriverNotInRace");
    }

    [Fact]
    public async Task Handle_UnclassifiedSession_ReturnsRaceNotFound()
    {
        using var db = InMemoryDb.Create();
        await SeedAsync(db);

        // Session 20 is the upcoming race from SeasonSeed, with no feature rows.
        var result = await Handler(db, new FakeChatClient(), aiAvailable: false).Handle(new(20, 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Prediction.RaceNotFound");
    }

    [Fact]
    public async Task Handle_ModelsNotTrained_ReturnsModelsNotTrained()
    {
        using var db = InMemoryDb.Create();

        var result = await Handler(db, new FakeChatClient(), aiAvailable: false, modelsAvailable: false).Handle(new(ClassifiedSessionKey, 1), CancellationToken.None);

        result.Error.Code.Should().Be("Prediction.ModelsNotTrained");
    }
}
