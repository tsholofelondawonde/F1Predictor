using F1Predictor.Application.Features.Analysis.GenerateRacePreview;
using F1Predictor.Application.Features.Analysis.RefreshAnalysis;
using F1Predictor.Application.Tests.Fakes;
using F1Predictor.Domain.Analysis.Entities;
using F1Predictor.Domain.RaceData.Entities;
using F1Predictor.Infrastructure.Database;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace F1Predictor.Application.Tests.Analysis;

public sealed class RefreshAnalysisCommandHandlerTests
{
    private static (RefreshAnalysisCommandHandler Handler, FakeChatClient Chat) BuildHandler(
        ApplicationDbContext db, bool aiAvailable, string replyText = "# Headline\n\nBody.")
    {
        var ai = new FakeAiCapabilities(aiAvailable);
        var chat = new FakeChatClient { ReplyText = replyText };
        var generatePreview = new GenerateRacePreviewCommandHandler(
            db, new FakeRacePredictor(), ai, chat,
            new ServiceCollection().AddHybridCache().Services.BuildServiceProvider().GetRequiredService<HybridCache>(),
            new FakeDateTimeProvider(), NullLogger<GenerateRacePreviewCommandHandler>.Instance);

        return (new RefreshAnalysisCommandHandler(db, generatePreview, ai), chat);
    }

    private static RacePreviewNarrative ExistingNarrative(bool gridConfirmed, int? basedOnLatestClassifiedSessionKey) => new()
    {
        SessionKey = 20,
        GridConfirmed = gridConfirmed,
        BasedOnLatestClassifiedSessionKey = basedOnLatestClassifiedSessionKey,
        Model = "old-model",
        GeneratedAt = DateTimeOffset.UtcNow.AddDays(-1),
        Headline = "Old headline",
        Content = "Old content"
    };

    [Fact]
    public async Task Handle_ChatUnavailable_SkipsAllAndNotes()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: true);
        var (handler, chat) = BuildHandler(db, aiAvailable: false);

        var result = await handler.Handle(new RefreshAnalysisCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PreviewsGenerated.Should().Be(0);
        result.Value.RacesIndexed.Should().Be(0);
        result.Value.Skipped.Should().Be(1);
        result.Value.Notes.Should().ContainSingle(n => n.Contains("Chat unavailable", StringComparison.Ordinal));
        chat.Calls.Should().BeEmpty();
        db.RacePreviewNarratives.ToList().Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NoYearGiven_CoversEveryIngestedYear()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: true); // Year 2026, one unclassified upcoming race.

        // A second season with no race sessions at all — NextRaceContext.LoadAsync has nothing
        // to find, so this year is a skip rather than a generation.
        db.Meetings.Add(new Meeting { MeetingKey = 100, Year = 2027, MeetingName = "Future season opener", CircuitShortName = "Z", CountryName = "Z", DateStart = DateTimeOffset.UtcNow.AddDays(200) });
        await db.SaveChangesAsync();

        var (handler, _) = BuildHandler(db, aiAvailable: true);

        var result = await handler.Handle(new RefreshAnalysisCommand { Year = null }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PreviewsGenerated.Should().Be(1);
        result.Value.Skipped.Should().Be(1);
        db.RacePreviewNarratives.ToList().Should().ContainSingle(n => n.SessionKey == 20);
    }

    [Fact]
    public async Task Handle_NoUpcomingRaceForYear_Skips()
    {
        using var db = InMemoryDb.Create();
        db.Meetings.Add(new Meeting { MeetingKey = 1, Year = 2025, MeetingName = "Only GP", CircuitShortName = "A", CountryName = "X", DateStart = DateTimeOffset.UtcNow.AddDays(-30) });
        db.RaceSessions.Add(new RaceSession { SessionKey = 10, MeetingKey = 1, SessionName = "Race", SessionType = "Race", DateStart = DateTimeOffset.UtcNow.AddDays(-30), IsClassified = true });
        await db.SaveChangesAsync();

        var (handler, chat) = BuildHandler(db, aiAvailable: true);

        var result = await handler.Handle(new RefreshAnalysisCommand { Year = 2025 }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PreviewsGenerated.Should().Be(0);
        result.Value.Skipped.Should().Be(1);
        result.Value.Notes.Should().BeEmpty();
        chat.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NoExistingPreview_Generates()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: true);
        var (handler, chat) = BuildHandler(db, aiAvailable: true);

        var result = await handler.Handle(new RefreshAnalysisCommand { Year = 2026 }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PreviewsGenerated.Should().Be(1);
        result.Value.Skipped.Should().Be(0);
        chat.Calls.Should().HaveCount(1);
        db.RacePreviewNarratives.Single().SessionKey.Should().Be(20);
    }

    [Fact]
    public async Task Handle_ProjectedGridNowConfirmed_Regenerates()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: true); // Real grid now exists -> next.GridConfirmed == true.
        db.RacePreviewNarratives.Add(ExistingNarrative(gridConfirmed: false, basedOnLatestClassifiedSessionKey: 10));
        await db.SaveChangesAsync();
        var (handler, chat) = BuildHandler(db, aiAvailable: true);

        var result = await handler.Handle(new RefreshAnalysisCommand { Year = 2026 }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PreviewsGenerated.Should().Be(1);
        chat.Calls.Should().HaveCount(1);
        db.RacePreviewNarratives.Single().Content.Should().NotBe("Old content");
    }

    [Fact]
    public async Task Handle_NewerRaceClassifiedSinceGeneration_Regenerates()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: true); // Latest classified session for 2026 is 10.
        db.RacePreviewNarratives.Add(ExistingNarrative(gridConfirmed: true, basedOnLatestClassifiedSessionKey: 5));
        await db.SaveChangesAsync();
        var (handler, chat) = BuildHandler(db, aiAvailable: true);

        var result = await handler.Handle(new RefreshAnalysisCommand { Year = 2026 }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PreviewsGenerated.Should().Be(1);
        chat.Calls.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_PreviewAlreadyFresh_Skips()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: true);
        db.RacePreviewNarratives.Add(ExistingNarrative(gridConfirmed: true, basedOnLatestClassifiedSessionKey: 10));
        await db.SaveChangesAsync();
        var (handler, chat) = BuildHandler(db, aiAvailable: true);

        var result = await handler.Handle(new RefreshAnalysisCommand { Year = 2026 }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PreviewsGenerated.Should().Be(0);
        result.Value.Skipped.Should().Be(1);
        chat.Calls.Should().BeEmpty();
        db.RacePreviewNarratives.Single().Content.Should().Be("Old content");
    }

    [Fact]
    public async Task Handle_PreviewGenerationFails_AddsNoteAndDoesNotCountAsGenerated()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: true);
        var (handler, chat) = BuildHandler(db, aiAvailable: true);
        chat.Throws = new FormatException("model 'llama3.1:8b' not found");

        var result = await handler.Handle(new RefreshAnalysisCommand { Year = 2026 }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PreviewsGenerated.Should().Be(0);
        result.Value.Skipped.Should().Be(0);
        result.Value.Notes.Should().ContainSingle(n => n.Contains("2026", StringComparison.Ordinal) && n.Contains("Analysis.ProviderFailed", StringComparison.Ordinal));
        db.RacePreviewNarratives.ToList().Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_RerunWithNothingChanged_IsIdempotent()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: true);
        var (handler, _) = BuildHandler(db, aiAvailable: true);

        var first = await handler.Handle(new RefreshAnalysisCommand { Year = 2026 }, CancellationToken.None);
        var second = await handler.Handle(new RefreshAnalysisCommand { Year = 2026 }, CancellationToken.None);

        first.Value.PreviewsGenerated.Should().Be(1);
        second.Value.PreviewsGenerated.Should().Be(0);
        second.Value.Skipped.Should().Be(1);
        db.RacePreviewNarratives.ToList().Should().HaveCount(1);
    }
}
