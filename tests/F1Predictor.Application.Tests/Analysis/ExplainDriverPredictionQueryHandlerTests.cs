using F1Predictor.Application.Features.Analysis.ExplainDriverPrediction;
using F1Predictor.Application.Tests.Fakes;
using F1Predictor.Infrastructure.Database;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace F1Predictor.Application.Tests.Analysis;

public sealed class ExplainDriverPredictionQueryHandlerTests
{
    private static ExplainDriverPredictionQueryHandler Handler(ApplicationDbContext db, FakeChatClient chat, bool aiAvailable, bool modelsAvailable = true) =>
        new(db, new FakeRacePredictor { ModelsAvailable = modelsAvailable }, new FakeAiCapabilities(aiAvailable), chat,
            new ServiceCollection().AddHybridCache().Services.BuildServiceProvider().GetRequiredService<HybridCache>(),
            NullLogger<ExplainDriverPredictionQueryHandler>.Instance);

    [Fact]
    public async Task Handle_AiUnavailable_ReturnsStructuredExplanationWithoutNarrative()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: false);
        var chat = new FakeChatClient();

        var result = await Handler(db, chat, aiAvailable: false).Handle(new(2026, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Narrative.Should().BeNull();
        result.Value.Model.Should().BeNull();
        result.Value.Podium.Contributions.Should().HaveCount(5);
        result.Value.Podium.Contributions.Single(c => c.Feature == "GridPosition").Direction.Should().Be("helps");
        result.Value.Podium.Contributions.Single(c => c.Feature == "AvgPitStopDuration").Direction.Should().Be("hurts");
        result.Value.Podium.Contributions.Single(c => c.Feature == "Rainfall").Direction.Should().Be("neutral");
        result.Value.FinishPosition.Should().BeNull();
        chat.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_AiAvailable_AddsNarrativeAndCachesIt()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: true);
        var chat = new FakeChatClient { ReplyText = "Front row start helps." };
        var handler = Handler(db, chat, aiAvailable: true);

        var first = await handler.Handle(new(2026, 3), CancellationToken.None);
        var second = await handler.Handle(new(2026, 3), CancellationToken.None);

        first.Value.Narrative.Should().Be("Front row start helps.");
        first.Value.Model.Should().Be("fake-model");
        second.Value.Narrative.Should().Be("Front row start helps.");
        chat.Calls.Should().HaveCount(1);
        chat.Calls[0][0].Role.Should().Be(ChatRole.System);
        chat.Calls[0][1].Text.Should().Contain("GridPosition");
    }

    [Fact]
    public async Task Handle_ChatClientFails_StillReturnsStructuredExplanation()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: false);
        var chat = new FakeChatClient { Throws = new HttpRequestException("ollama down") };

        var result = await Handler(db, chat, aiAvailable: true).Handle(new(2026, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Narrative.Should().BeNull();
    }

    // OllamaSharp's own exception types (model not pulled, malformed body) cannot be named from
    // the Application layer; whatever the provider throws, the contributions must still come back.
    [Fact]
    public async Task Handle_NarrativeProviderThrowsUnlistedException_ReturnsContributionsWithNullNarrative()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: false);
        var chat = new FakeChatClient { Throws = new FormatException("model 'llama3.1:8b' not found") };

        var result = await Handler(db, chat, aiAvailable: true).Handle(new(2026, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Narrative.Should().BeNull();
        result.Value.Model.Should().BeNull();
        result.Value.Podium.Contributions.Should().HaveCount(5);
    }

    [Fact]
    public async Task Handle_IncludeNarrativeFalse_SkipsChatClient()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: true);
        var chat = new FakeChatClient();

        var result = await Handler(db, chat, aiAvailable: true).Handle(new(2026, 3, IncludeNarrative: false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Narrative.Should().BeNull();
        result.Value.Model.Should().BeNull();
        result.Value.Podium.Contributions.Should().HaveCount(5);
        chat.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_RequestCancelled_PropagatesCancellation()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: false);
        var chat = new FakeChatClient();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => Handler(db, chat, aiAvailable: true).Handle(new(2026, 1), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Handle_DriverNotEntered_ReturnsNotFound()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: false);

        var result = await Handler(db, new FakeChatClient(), aiAvailable: false).Handle(new(2026, 99), CancellationToken.None);

        result.Error.Code.Should().Be("Analysis.DriverNotEntered");
    }

    [Fact]
    public async Task Handle_ModelsNotTrained_ReturnsModelsNotTrained()
    {
        using var db = InMemoryDb.Create();

        var result = await Handler(db, new FakeChatClient(), aiAvailable: false, modelsAvailable: false).Handle(new(2026, 1), CancellationToken.None);

        result.Error.Code.Should().Be("Prediction.ModelsNotTrained");
    }
}
