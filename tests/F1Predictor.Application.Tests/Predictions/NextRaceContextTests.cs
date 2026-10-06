using F1Predictor.Application.Features.Predictions.PreviewNextRace;
using F1Predictor.Application.Tests.Fakes;
using FluentAssertions;
using Xunit;

namespace F1Predictor.Application.Tests.Predictions;

public sealed class NextRaceContextTests
{
    private const int Year = 2026;

    [Fact]
    public async Task LoadAsync_NoGridYet_ProjectsFromFormAndFlagsUnconfirmed()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: false);

        var result = await NextRaceContext.LoadAsync(db, Year, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Race.SessionKey.Should().Be(20);
        result.Value.GridConfirmed.Should().BeFalse();
        result.Value.Features[1].GridPosition.Should().Be(1);
        result.Value.Features[3].GridPosition.Should().Be(3);
        result.Value.History.Should().ContainKeys(1, 2, 3);
    }

    [Fact]
    public async Task LoadAsync_GridPublished_UsesRealOrder()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: true);

        var result = await NextRaceContext.LoadAsync(db, Year, CancellationToken.None);

        result.Value.GridConfirmed.Should().BeTrue();
        result.Value.Features[3].GridPosition.Should().Be(1);
        result.Value.Features[2].QualiGapToPole.Should().BeApproximately(0.9f, 1e-4f);
    }

    [Fact]
    public async Task LoadAsync_NothingUpcoming_ReturnsNoUpcomingRace()
    {
        using var db = InMemoryDb.Create();

        var result = await NextRaceContext.LoadAsync(db, Year, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Prediction.NoUpcomingRace");
    }
}
