using F1Predictor.Application.Features.Predictions.PredictRace;
using F1Predictor.Application.Tests.Fakes;
using F1Predictor.Domain.RaceData.Entities;
using FluentAssertions;
using Xunit;

namespace F1Predictor.Application.Tests.Predictions;

public sealed class ClassifiedRaceLoaderTests
{
    [Fact]
    public async Task LoadAsync_ClassifiedSessionWithFeatures_ReturnsThem()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: false);
        // SeasonSeed only adds driver entries for the upcoming session (20); add them for the
        // already-classified one (10) too, so the directory is populated for this test.
        db.DriverEntries.Add(new DriverEntry { SessionKey = 10, DriverNumber = 1, FullName = "Driver 1", NameAcronym = "D01", TeamName = "Team", TeamColour = "FF0000" });
        await db.SaveChangesAsync();

        var result = await ClassifiedRace.LoadAsync(db, sessionKey: 10, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.MeetingName.Should().Be("Past GP");
        result.Value.Features.Should().ContainKeys(1, 2, 3);
        result.Value.Features[1].FinishPosition.Should().Be(1);
        result.Value.Directory[1].NameAcronym.Should().Be("D01");
    }

    [Fact]
    public async Task LoadAsync_UnknownSession_ReturnsRaceNotFound()
    {
        using var db = InMemoryDb.Create();

        var result = await ClassifiedRace.LoadAsync(db, sessionKey: 999, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Prediction.RaceNotFound");
    }

    [Fact]
    public async Task LoadAsync_UnclassifiedSession_ReturnsRaceNotFound()
    {
        using var db = InMemoryDb.Create();
        // Session 20 is the upcoming, not-yet-run race from SeasonSeed — it has no feature rows.
        await SeasonSeed.SeedNextRaceAsync(db, withGrid: false);

        var result = await ClassifiedRace.LoadAsync(db, sessionKey: 20, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Prediction.RaceNotFound");
    }
}
