using F1Predictor.Application.Features.Predictions;
using F1Predictor.Application.Features.Predictions.Evaluation;
using F1Predictor.Domain.Predictions;
using FluentAssertions;
using Xunit;

namespace F1Predictor.Application.Tests.Predictions;

public sealed class RaceTimeSplitTests
{
    private static readonly DateTimeOffset SeasonStart = new(2025, 3, 1, 13, 0, 0, TimeSpan.Zero);

    /// <summary>Races 1..count, one week apart, session key == round number.</summary>
    private static List<SeasonRace> Races(int count) =>
        [.. Enumerable.Range(1, count).Select(round => new SeasonRace(
            round, round, $"GP {round}", "X", SeasonStart.AddDays(7 * (round - 1)), 2025))];

    private static List<DriverRaceFeature> RowsFor(IEnumerable<int> sessionKeys, int driversPerRace = 3) =>
        [.. sessionKeys.SelectMany(key => Enumerable.Range(1, driversPerRace).Select(driver =>
            new DriverRaceFeature { SessionKey = key, DriverNumber = driver, GridPosition = driver, FinishPosition = driver }))];

    [Fact]
    public void Split_TenRaces_ValidatesOnTheLatestTwoAndTrainsOnTheRest()
    {
        var races = Races(10);

        var split = RaceTimeSplit.Split(RowsFor(races.Select(r => r.SessionKey)), races);

        split.ValidationRaces.Select(r => r.SessionKey).Should().BeEquivalentTo([9, 10]);
        split.TrainingRaces.Select(r => r.SessionKey).Should().BeEquivalentTo(Enumerable.Range(1, 8));
        split.Data.Train.Should().HaveCount(8 * 3);
        split.Data.Validation.Should().HaveCount(2 * 3);
    }

    [Fact]
    public void Split_TwentyRaces_ValidatesOnTwentyPercent()
    {
        var races = Races(20);

        var split = RaceTimeSplit.Split(RowsFor(races.Select(r => r.SessionKey)), races);

        split.ValidationRaces.Should().HaveCount(4);
        split.TrainingRaces.Should().HaveCount(16);
    }

    [Fact]
    public void Split_NoRaceEverAppearsOnBothSides_AndValidationIsStrictlyLater()
    {
        var races = Races(13);

        var split = RaceTimeSplit.Split(RowsFor(races.Select(r => r.SessionKey)), races);

        var trainKeys = split.Data.Train.Select(f => f.SessionKey).ToHashSet();
        var validationKeys = split.Data.Validation.Select(f => f.SessionKey).ToHashSet();
        trainKeys.Should().NotIntersectWith(validationKeys);

        split.ValidationRaces.Min(r => r.DateStart).Should().BeAfter(split.TrainingRaces.Max(r => r.DateStart));
    }

    [Fact]
    public void Split_OrderOfInputRacesDoesNotMatter()
    {
        var races = Races(10);
        var shuffled = races.OrderByDescending(r => r.SessionKey % 3).ThenBy(r => -r.SessionKey).ToList();

        var split = RaceTimeSplit.Split(RowsFor(races.Select(r => r.SessionKey)), shuffled);

        split.ValidationRaces.Select(r => r.SessionKey).Should().BeEquivalentTo([9, 10]);
    }

    [Fact]
    public void Split_RowsFromRacesNotInTheList_AreLeftOutOfBothSides()
    {
        // The holdout race's rows are in the feature set but it is not passed as a race to split.
        var races = Races(10);
        const int holdoutKey = 99;

        var split = RaceTimeSplit.Split(RowsFor([.. races.Select(r => r.SessionKey), holdoutKey]), races);

        split.Data.Train.Should().NotContain(f => f.SessionKey == holdoutKey);
        split.Data.Validation.Should().NotContain(f => f.SessionKey == holdoutKey);
    }

    [Fact]
    public void Split_AcrossSeasons_OrdersByDateNotBySessionKey()
    {
        // Session keys are not chronological across seasons; dates are.
        List<SeasonRace> races =
        [
            new(9_000, 1, "2024 GP A", "X", new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero), 2024),
            new(9_001, 2, "2024 GP B", "X", new DateTimeOffset(2024, 4, 1, 0, 0, 0, TimeSpan.Zero), 2024),
            new(1_000, 3, "2025 GP A", "X", new DateTimeOffset(2025, 3, 1, 0, 0, 0, TimeSpan.Zero), 2025),
            new(1_001, 4, "2025 GP B", "X", new DateTimeOffset(2025, 4, 1, 0, 0, 0, TimeSpan.Zero), 2025),
        ];

        var split = RaceTimeSplit.Split(RowsFor(races.Select(r => r.SessionKey)), races);

        split.ValidationRaces.Select(r => r.SessionKey).Should().BeEquivalentTo([1_000, 1_001]);
    }

    [Fact]
    public void Split_FewerThanMinimumRaces_Throws()
    {
        // The handler guards on MinimumRaces first; reaching here with fewer is a programmer error.
        var races = Races(RaceTimeSplit.MinimumRaces - 1);

        var act = () => RaceTimeSplit.Split(RowsFor(races.Select(r => r.SessionKey)), races);

        act.Should().Throw<ArgumentException>();
    }
}
