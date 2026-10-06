using F1Predictor.Domain.Predictions;
using F1Predictor.Domain.RaceData.Entities;
using FluentAssertions;
using Xunit;

namespace F1Predictor.Application.Tests.Predictions;

/// <summary>
/// Pins down the feature rules before anyone changes them — adding, dropping or re-imputing a
/// feature should break exactly the test that describes the old rule, not pass silently.
/// </summary>
public sealed class DriverRaceFeatureTests
{
    private const double PolePace = 80.0;

    private static SessionResultEntry Result(int position, bool dsq = false) =>
        new() { SessionKey = 1, DriverNumber = 44, Position = position, Dsq = dsq };

    [Fact]
    public void Create_OnTheGridWithATime_TakesGridSlotAndGapToPole()
    {
        var feature = DriverRaceFeature.Create(
            Result(5),
            new StartingGridEntry { DriverNumber = 44, Position = 4, LapDuration = 80.75 },
            gridCount: 20, PolePace, driverPits: [], rainedInSession: false);

        feature.GridPosition.Should().Be(4);
        feature.QualiGapToPole.Should().BeApproximately(0.75f, 1e-5f);
    }

    [Fact]
    public void Create_NoGridEntry_StartsBehindTheWholeField()
    {
        var feature = DriverRaceFeature.Create(Result(15), gridEntry: null, gridCount: 20, PolePace, driverPits: [], rainedInSession: false);

        feature.GridPosition.Should().Be(21);
        feature.QualiGapToPole.Should().Be((float)DriverRaceFeature.MissingLapTimeSentinel);
    }

    [Fact]
    public void Create_OnTheGridWithoutATime_UsesTheSentinelGap()
    {
        var feature = DriverRaceFeature.Create(
            Result(12),
            new StartingGridEntry { DriverNumber = 44, Position = 18, LapDuration = null },
            gridCount: 20, PolePace, driverPits: [], rainedInSession: false);

        feature.GridPosition.Should().Be(18);
        feature.QualiGapToPole.Should().Be((float)DriverRaceFeature.MissingLapTimeSentinel);
    }

    [Theory]
    [InlineData(1, false, true, true)]
    [InlineData(3, false, true, true)]
    [InlineData(4, false, false, true)]
    [InlineData(10, false, false, true)]
    [InlineData(11, false, false, false)]
    [InlineData(2, true, false, false)] // disqualified: the classification position no longer counts
    public void Create_Labels_FollowPositionAndDisqualification(int position, bool dsq, bool podium, bool points)
    {
        var feature = DriverRaceFeature.Create(Result(position, dsq), gridEntry: null, gridCount: 20, PolePace, driverPits: [], rainedInSession: false);

        feature.Podium.Should().Be(podium);
        feature.PointsFinish.Should().Be(points);
        feature.FinishPosition.Should().Be(position);
    }

    [Fact]
    public void Create_PitStops_CountAndAverageThePreferredDuration()
    {
        PitStopEntry[] pits =
        [
            new() { DriverNumber = 44, StopDuration = 2.0, LaneDuration = 22.0 },
            new() { DriverNumber = 44, StopDuration = null, LaneDuration = 24.0 }, // pre-2024 US GP: lane time only
        ];

        var feature = DriverRaceFeature.Create(Result(5), gridEntry: null, gridCount: 20, PolePace, pits, rainedInSession: true);

        feature.PitStopCount.Should().Be(2);
        feature.AvgPitStopDuration.Should().BeApproximately(13.0f, 1e-5f);
        feature.Rainfall.Should().Be(1);
    }

    [Fact]
    public void Create_UnclassifiedDriver_Throws()
    {
        var unclassified = new SessionResultEntry { SessionKey = 1, DriverNumber = 44, Position = null };

        var act = () => DriverRaceFeature.Create(unclassified, gridEntry: null, gridCount: 20, PolePace, driverPits: [], rainedInSession: false);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PolePaceOf_IgnoresDriversWithoutATime()
    {
        StartingGridEntry[] grid =
        [
            new() { Position = 1, LapDuration = null },
            new() { Position = 2, LapDuration = 81.2 },
            new() { Position = 3, LapDuration = 80.9 },
        ];

        DriverRaceFeature.PolePaceOf(grid).Should().Be(80.9);
    }
}
