using F1Predictor.Application.Features.Analysis;
using F1Predictor.Application.Features.Analysis.GenerateRacePreview;
using F1Predictor.Application.Tests.Fakes;
using F1Predictor.Infrastructure.Database;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace F1Predictor.Application.Tests.Analysis;

public sealed class GenerateRacePreviewCommandHandlerTests
{
    private static readonly DateTime FixedUtcNow = DateTime.SpecifyKind(new DateTime(2026, 9, 17, 10, 0, 0), DateTimeKind.Utc);

    private static GenerateRacePreviewCommandHandler Handler(ApplicationDbContext db, FakeChatClient chat, bool aiAvailable, bool modelsAvailable = true) =>
        new(db, new FakeRacePredictor { ModelsAvailable = modelsAvailable }, new FakeAiCapabilities(aiAvailable), chat,
            new ServiceCollection().AddHybridCache().Services.BuildServiceProvider().GetRequiredService<HybridCache>(),
            new FakeDateTimeProvider { UtcNow = FixedUtcNow });

    [Fact]
    public async Task Handle_AiUnavailable_ReturnsAiUnavailable()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: true);
        var chat = new FakeChatClient();

        var result = await Handler(db, chat, aiAvailable: false).Handle(new GenerateRacePreviewCommand { SessionKey = 20 }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Analysis.AiUnavailable");
        chat.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ModelsNotTrained_ReturnsModelsNotTrained()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: true);

        var result = await Handler(db, new FakeChatClient(), aiAvailable: true, modelsAvailable: false)
            .Handle(new GenerateRacePreviewCommand { SessionKey = 20 }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Prediction.ModelsNotTrained");
    }

    [Fact]
    public async Task Handle_SessionNotIngested_ReturnsRaceNotFound()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: true);

        var result = await Handler(db, new FakeChatClient(), aiAvailable: true).Handle(new GenerateRacePreviewCommand { SessionKey = 999 }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Analysis.RaceNotFound");
    }

    [Fact]
    public async Task Handle_SessionIsNotNextRace_ReturnsNotNextRace()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: true);

        var result = await Handler(db, new FakeChatClient(), aiAvailable: true).Handle(new GenerateRacePreviewCommand { SessionKey = 10 }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Analysis.NotNextRace");
    }

    [Fact]
    public async Task Handle_FirstGeneration_InsertsRowAndReturnsHeadline()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: true);
        var chat = new FakeChatClient { ReplyText = "# Front row favourites\n\n## Podium candidates\n..." };

        var result = await Handler(db, chat, aiAvailable: true).Handle(new GenerateRacePreviewCommand { SessionKey = 20 }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Headline.Should().Be("Front row favourites");

        var rows = db.RacePreviewNarratives.ToList();
        rows.Should().HaveCount(1);
        var row = rows[0];
        row.SessionKey.Should().Be(20);
        row.Headline.Should().Be("Front row favourites");
        row.Model.Should().Be("fake-model");
        row.GeneratedAt.Should().Be(new DateTimeOffset(FixedUtcNow, TimeSpan.Zero));
        row.BasedOnLatestClassifiedSessionKey.Should().Be(10);
        row.GridConfirmed.Should().BeTrue();

        chat.Calls.Should().HaveCount(1);
        chat.Calls[0][0].Role.Should().Be(ChatRole.System);
        chat.Calls[0][0].Text.Should().Be(AnalystPrompts.RacePreviewSystem());
        chat.Calls[0][1].Text.Should().Contain("\"gridConfirmed\"");
    }

    [Fact]
    public async Task Handle_SecondGeneration_ReplacesRow()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: true);
        var chat = new FakeChatClient { ReplyText = "# First\n\nSome content." };
        var handler = Handler(db, chat, aiAvailable: true);

        await handler.Handle(new GenerateRacePreviewCommand { SessionKey = 20 }, CancellationToken.None);
        chat.ReplyText = "# Second\n\nDifferent content.";
        var second = await handler.Handle(new GenerateRacePreviewCommand { SessionKey = 20 }, CancellationToken.None);

        second.IsSuccess.Should().BeTrue();
        db.RacePreviewNarratives.ToList().Should().HaveCount(1);
        db.RacePreviewNarratives.Single().Content.Should().Be("# Second\n\nDifferent content.");
    }

    [Fact]
    public async Task Handle_EmptyReply_ReturnsModelRefused()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: true);
        var chat = new FakeChatClient { ReplyText = "   " };

        var result = await Handler(db, chat, aiAvailable: true).Handle(new GenerateRacePreviewCommand { SessionKey = 20 }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Analysis.ModelRefused");
        db.RacePreviewNarratives.ToList().Should().BeEmpty();
    }
}
