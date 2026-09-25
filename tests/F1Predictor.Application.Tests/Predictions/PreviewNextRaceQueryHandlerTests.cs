using F1Predictor.Application.Abstractions.OpenF1;
using F1Predictor.Application.Features.Predictions.PreviewNextRace;
using F1Predictor.Application.Tests.Fakes;
using F1Predictor.Infrastructure.Database;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace F1Predictor.Application.Tests.Predictions;

public sealed class PreviewNextRaceQueryHandlerTests
{
    private const int Year = 2026;
    private const int NextRaceMeetingKey = 2;

    private static PreviewNextRaceQueryHandler Handler(ApplicationDbContext db, FakeOpenF1Client openF1) =>
        new(db, new FakeRacePredictor(), openF1,
            new ServiceCollection().AddHybridCache().Services.BuildServiceProvider().GetRequiredService<HybridCache>(),
            NullLogger<PreviewNextRaceQueryHandler>.Instance);

    private static OpenF1Session QualifyingSession(int sessionKey = 200) =>
        new() { SessionKey = sessionKey, MeetingKey = NextRaceMeetingKey, SessionType = "Qualifying", SessionName = "Qualifying" };

    [Fact]
    public async Task Handle_GridConfirmed_SkipsOpenF1CheckAndReturnsFalse()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: true);
        var openF1 = new FakeOpenF1Client { Sessions = [QualifyingSession()], Grid = [new OpenF1StartingGrid { DriverNumber = 1, Position = 1 }] };

        var result = await Handler(db, openF1).Handle(new PreviewNextRaceQuery(Year), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.QualifyingReadyToIngest.Should().BeFalse();
        openF1.GetSessionsCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_GridUnconfirmed_OpenF1HasGrid_ReturnsTrue()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: false);
        var openF1 = new FakeOpenF1Client { Sessions = [QualifyingSession()], Grid = [new OpenF1StartingGrid { DriverNumber = 1, Position = 1 }] };

        var result = await Handler(db, openF1).Handle(new PreviewNextRaceQuery(Year), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.QualifyingReadyToIngest.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_GridUnconfirmed_OpenF1GridEmpty_ReturnsFalse()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: false);
        var openF1 = new FakeOpenF1Client { Sessions = [QualifyingSession()], Grid = [] };

        var result = await Handler(db, openF1).Handle(new PreviewNextRaceQuery(Year), CancellationToken.None);

        result.Value.QualifyingReadyToIngest.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_GridUnconfirmed_NoQualifyingSessionOnOpenF1_ReturnsFalse()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: false);
        var openF1 = new FakeOpenF1Client { Sessions = [] };

        var result = await Handler(db, openF1).Handle(new PreviewNextRaceQuery(Year), CancellationToken.None);

        result.Value.QualifyingReadyToIngest.Should().BeFalse();
        openF1.GetStartingGridCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_GridUnconfirmed_OpenF1Unreachable_ReturnsFalseWithoutFailingRequest()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: false);
        var openF1 = new FakeOpenF1Client { Throws = new HttpRequestException("openf1 down") };

        var result = await Handler(db, openF1).Handle(new PreviewNextRaceQuery(Year), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.QualifyingReadyToIngest.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_GridUnconfirmed_CalledTwice_CachesOpenF1Result()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: false);
        var openF1 = new FakeOpenF1Client { Sessions = [QualifyingSession()], Grid = [new OpenF1StartingGrid { DriverNumber = 1, Position = 1 }] };
        var handler = Handler(db, openF1);

        await handler.Handle(new PreviewNextRaceQuery(Year), CancellationToken.None);
        await handler.Handle(new PreviewNextRaceQuery(Year), CancellationToken.None);

        openF1.GetSessionsCallCount.Should().Be(1);
    }
}
