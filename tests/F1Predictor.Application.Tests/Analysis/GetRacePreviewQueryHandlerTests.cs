using F1Predictor.Application.Features.Analysis.GetRacePreview;
using F1Predictor.Application.Tests.Fakes;
using F1Predictor.Domain.Analysis.Entities;
using F1Predictor.Domain.RaceData.Entities;
using F1Predictor.Infrastructure.Database;
using FluentAssertions;
using Xunit;

namespace F1Predictor.Application.Tests.Analysis;

public sealed class GetRacePreviewQueryHandlerTests
{
    [Fact]
    public async Task Handle_NoRow_ReturnsPreviewNotFound()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: true);

        var result = await new GetRacePreviewQueryHandler(db).Handle(new GetRacePreviewQuery(20), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Analysis.PreviewNotFound");
    }

    [Fact]
    public async Task Handle_RowExists_ReturnsItNotStale()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: true);
        db.RacePreviewNarratives.Add(new RacePreviewNarrative
        {
            SessionKey = 20,
            GridConfirmed = true,
            BasedOnLatestClassifiedSessionKey = 10,
            Model = "fake-model",
            GeneratedAt = DateTimeOffset.UtcNow,
            Headline = "Headline",
            Content = "Content"
        });
        await db.SaveChangesAsync();

        var result = await new GetRacePreviewQueryHandler(db).Handle(new GetRacePreviewQuery(20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Stale.Should().BeFalse();
        result.Value.Headline.Should().Be("Headline");
        result.Value.MeetingName.Should().Be("Next GP");
    }

    [Fact]
    public async Task Handle_ProjectedGridButGridNowPublished_FlagsStale()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: false);
        db.RacePreviewNarratives.Add(new RacePreviewNarrative
        {
            SessionKey = 20,
            GridConfirmed = false,
            BasedOnLatestClassifiedSessionKey = 10,
            Model = "fake-model",
            GeneratedAt = DateTimeOffset.UtcNow,
            Headline = "Headline",
            Content = "Content"
        });
        await db.SaveChangesAsync();

        db.StartingGridEntries.Add(new StartingGridEntry { SessionKey = 20, DriverNumber = 1, Position = 1, LapDuration = 80.0 });
        await db.SaveChangesAsync();

        var result = await new GetRacePreviewQueryHandler(db).Handle(new GetRacePreviewQuery(20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Stale.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NewerClassifiedSession_FlagsStale()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: true);
        db.RacePreviewNarratives.Add(new RacePreviewNarrative
        {
            SessionKey = 20,
            GridConfirmed = true,
            BasedOnLatestClassifiedSessionKey = 10,
            Model = "fake-model",
            GeneratedAt = DateTimeOffset.UtcNow,
            Headline = "Headline",
            Content = "Content"
        });
        await db.SaveChangesAsync();

        db.RaceSessions.Add(new RaceSession { SessionKey = 15, MeetingKey = 1, SessionName = "Race", SessionType = "Race", DateStart = DateTimeOffset.UtcNow.AddDays(-7), IsClassified = true });
        await db.SaveChangesAsync();

        var result = await new GetRacePreviewQueryHandler(db).Handle(new GetRacePreviewQuery(20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Stale.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ClassifiedRaceInAnotherSeason_IsNotStale()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: true);
        await SeasonSeed.SeedOtherSeasonClassifiedRaceAsync(db, year: 2025, sessionKey: 900);
        db.RacePreviewNarratives.Add(new RacePreviewNarrative
        {
            SessionKey = 20,
            GridConfirmed = true,
            BasedOnLatestClassifiedSessionKey = 10,
            Model = "fake-model",
            GeneratedAt = DateTimeOffset.UtcNow,
            Headline = "Headline",
            Content = "Content"
        });
        await db.SaveChangesAsync();

        var result = await new GetRacePreviewQueryHandler(db).Handle(new GetRacePreviewQuery(20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Stale.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_GeneratedBeforeAnyResult_BecomesStaleOnceARaceIsClassified()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: true);
        db.RacePreviewNarratives.Add(new RacePreviewNarrative
        {
            SessionKey = 20,
            GridConfirmed = true,
            BasedOnLatestClassifiedSessionKey = null,
            Model = "fake-model",
            GeneratedAt = DateTimeOffset.UtcNow,
            Headline = "Headline",
            Content = "Content"
        });
        await db.SaveChangesAsync();

        var result = await new GetRacePreviewQueryHandler(db).Handle(new GetRacePreviewQuery(20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Stale.Should().BeTrue();
    }
}
