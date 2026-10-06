using F1Predictor.Domain.Analysis;
using F1Predictor.Domain.RaceData.Entities;
using FluentAssertions;
using Xunit;

namespace F1Predictor.Application.Tests.Domain;

/// <summary>
/// Golden-file tests for <see cref="RaceFactSheet.Build"/>: <see cref="RaceFactSheet.Text"/> is
/// fixed-order, fixed-wording, so these assert the *exact* rendered string, not a substring —
/// that is the point of a golden test. If the wording is deliberately changed, these strings (and
/// only these strings) should change with it.
/// </summary>
public sealed class RaceFactSheetTests
{
    private static Meeting BaseMeeting() => new()
    {
        MeetingKey = 1,
        Year = 2025,
        CircuitShortName = "Monaco",
        CountryName = "Monaco",
        MeetingName = "Monaco Grand Prix",
        DateStart = new DateTimeOffset(2025, 5, 23, 0, 0, 0, TimeSpan.Zero)
    };

    private static RaceSession BaseSession() => new()
    {
        SessionKey = 100,
        MeetingKey = 1,
        SessionName = "Race",
        SessionType = "Race",
        DateStart = new DateTimeOffset(2025, 5, 25, 13, 0, 0, TimeSpan.Zero),
        IsSprint = false,
        IsClassified = true
    };

    private static DriverEntry Driver(int number, string acronym, string team) => new()
    {
        SessionKey = 100,
        DriverNumber = number,
        FullName = acronym,
        NameAcronym = acronym,
        TeamName = team,
        TeamColour = "000000"
    };

    // Five-driver field: NOR wins from pole, HAM is the biggest gainer (P5->P4), VER the
    // biggest loser (P4->P5). Nobody retires, nobody starts from the pit lane.
    private const int Nor = 4;
    private const int Lec = 16;
    private const int Pia = 81;
    private const int Ham = 44;
    private const int Ver = 1;

    private static Dictionary<int, DriverEntry> BaseDirectory() => new()
    {
        [Nor] = Driver(Nor, "NOR", "McLaren"),
        [Lec] = Driver(Lec, "LEC", "Ferrari"),
        [Pia] = Driver(Pia, "PIA", "McLaren"),
        [Ham] = Driver(Ham, "HAM", "Ferrari"),
        [Ver] = Driver(Ver, "VER", "Red Bull Racing")
    };

    private static IReadOnlyList<StartingGridEntry> BaseGrid() =>
    [
        new StartingGridEntry { SessionKey = 100, DriverNumber = Nor, Position = 1, LapDuration = 70.1 },
        new StartingGridEntry { SessionKey = 100, DriverNumber = Lec, Position = 2, LapDuration = 70.4 },
        new StartingGridEntry { SessionKey = 100, DriverNumber = Pia, Position = 3, LapDuration = 70.6 },
        new StartingGridEntry { SessionKey = 100, DriverNumber = Ver, Position = 4, LapDuration = 70.7 },
        new StartingGridEntry { SessionKey = 100, DriverNumber = Ham, Position = 5, LapDuration = 70.9 }
    ];

    private static IReadOnlyList<SessionResultEntry> BaseResults() =>
    [
        new SessionResultEntry { SessionKey = 100, DriverNumber = Nor, Position = 1, Points = 25 },
        new SessionResultEntry { SessionKey = 100, DriverNumber = Lec, Position = 2, Points = 18 },
        new SessionResultEntry { SessionKey = 100, DriverNumber = Pia, Position = 3, Points = 15 },
        new SessionResultEntry { SessionKey = 100, DriverNumber = Ham, Position = 4, Points = 12 },
        new SessionResultEntry { SessionKey = 100, DriverNumber = Ver, Position = 5, Points = 10 }
    ];

    private static IReadOnlyList<PitStopEntry> BasePitStops() =>
    [
        new PitStopEntry { SessionKey = 100, DriverNumber = Nor, StopDuration = 2.50 },
        new PitStopEntry { SessionKey = 100, DriverNumber = Lec, StopDuration = 2.80 },
        new PitStopEntry { SessionKey = 100, DriverNumber = Pia, StopDuration = 3.00 },
        new PitStopEntry { SessionKey = 100, DriverNumber = Ham, StopDuration = 2.31 },
        new PitStopEntry { SessionKey = 100, DriverNumber = Ver, StopDuration = 2.90 }
    ];

    private static IReadOnlyList<WeatherReading> DryWeather() =>
    [
        new WeatherReading { SessionKey = 100, Rainfall = 0, TrackTemperature = 45 }
    ];

    private static IReadOnlyList<WeatherReading> WetWeather() =>
    [
        new WeatherReading { SessionKey = 100, Rainfall = 1, TrackTemperature = 28 }
    ];

    private static RaceFactSheet BuildBase(IReadOnlyList<WeatherReading>? weather = null) =>
        RaceFactSheet.Build(BaseMeeting(), BaseSession(), BaseResults(), BaseGrid(), BasePitStops(), weather ?? DryWeather(), BaseDirectory());

    [Fact]
    public void Build_BaseFixture_ProducesExactGoldenText()
    {
        var sheet = BuildBase();

        sheet.Text.Should().Be(
            "2025 Monaco Grand Prix, Monaco, 25 May 2025. Dry. Pole NOR. " +
            "Winner NOR (McLaren) from P1; podium NOR, LEC, PIA. " +
            "Biggest gainer HAM P5→P4, biggest loser VER P4→P5. " +
            "0 retirements. Average 1.0 pit stops, fastest stop 2.31s (HAM)." +
            "\n\nPoints scorers by team:" +
            "\n- Ferrari: 2" +
            "\n- McLaren: 2" +
            "\n- Red Bull Racing: 1");

        sheet.Winner.Should().Be("NOR");
        sheet.Podium.Should().Equal("NOR", "LEC", "PIA");
        sheet.PoleSitter.Should().Be("NOR");
        sheet.Rained.Should().BeFalse();
        sheet.BiggestGainer.Should().Be("HAM P5→P4");
        sheet.BiggestLoser.Should().Be("VER P4→P5");
        sheet.Retirements.Should().Be(0);
        sheet.AveragePitStops.Should().Be(1.0);
        sheet.FastestStop.Should().Be("2.31s (HAM)");
        sheet.PointsScorersByTeam.Should().BeEquivalentTo(new Dictionary<string, int>
        {
            ["McLaren"] = 2,
            ["Ferrari"] = 2,
            ["Red Bull Racing"] = 1
        });
    }

    [Fact]
    public void Build_RainVariant_ProducesExactGoldenTextWithWetWording()
    {
        var sheet = BuildBase(WetWeather());

        sheet.Text.Should().Be(
            "2025 Monaco Grand Prix, Monaco, 25 May 2025. Wet. Pole NOR. " +
            "Winner NOR (McLaren) from P1; podium NOR, LEC, PIA. " +
            "Biggest gainer HAM P5→P4, biggest loser VER P4→P5. " +
            "0 retirements. Average 1.0 pit stops, fastest stop 2.31s (HAM)." +
            "\n\nPoints scorers by team:" +
            "\n- Ferrari: 2" +
            "\n- McLaren: 2" +
            "\n- Red Bull Racing: 1");

        sheet.Rained.Should().BeTrue();
    }

    [Fact]
    public void Build_DnfVariant_ProducesExactGoldenTextAndCountsTheRetirement()
    {
        var directory = new Dictionary<int, DriverEntry>
        {
            [Nor] = Driver(Nor, "NOR", "McLaren"),
            [Ver] = Driver(Ver, "VER", "Red Bull Racing"),
            [Lec] = Driver(Lec, "LEC", "Ferrari")
        };

        IReadOnlyList<StartingGridEntry> grid =
        [
            new StartingGridEntry { SessionKey = 100, DriverNumber = Nor, Position = 1, LapDuration = 70.1 },
            new StartingGridEntry { SessionKey = 100, DriverNumber = Ver, Position = 2, LapDuration = 70.3 },
            new StartingGridEntry { SessionKey = 100, DriverNumber = Lec, Position = 3, LapDuration = 70.5 }
        ];

        IReadOnlyList<SessionResultEntry> results =
        [
            new SessionResultEntry { SessionKey = 100, DriverNumber = Nor, Position = 1, Points = 25 },
            new SessionResultEntry { SessionKey = 100, DriverNumber = Ver, Position = null, Points = 0, Dnf = true },
            new SessionResultEntry { SessionKey = 100, DriverNumber = Lec, Position = 2, Points = 18 }
        ];

        IReadOnlyList<PitStopEntry> pitStops =
        [
            new PitStopEntry { SessionKey = 100, DriverNumber = Nor, StopDuration = 2.50 },
            new PitStopEntry { SessionKey = 100, DriverNumber = Ver, StopDuration = 3.00 },
            new PitStopEntry { SessionKey = 100, DriverNumber = Lec, StopDuration = 2.20 }
        ];

        var sheet = RaceFactSheet.Build(BaseMeeting(), BaseSession(), results, grid, pitStops, [], directory);

        sheet.Text.Should().Be(
            "2025 Monaco Grand Prix, Monaco, 25 May 2025. Dry. Pole NOR. " +
            "Winner NOR (McLaren) from P1; podium NOR, LEC. " +
            "Biggest gainer LEC P3→P2, biggest loser NOR P1→P1. " +
            "1 retirement. Average 1.0 pit stops, fastest stop 2.20s (LEC)." +
            "\n\nPoints scorers by team:" +
            "\n- Ferrari: 1" +
            "\n- McLaren: 1");

        sheet.Retirements.Should().Be(1);
        sheet.Podium.Should().Equal("NOR", "LEC");
    }

    [Fact]
    public void Build_PitLaneStartVariant_TreatsMissingGridEntryAsBehindTheWholeField()
    {
        var directory = new Dictionary<int, DriverEntry>
        {
            [Nor] = Driver(Nor, "NOR", "McLaren"),
            [Ver] = Driver(Ver, "VER", "Red Bull Racing"),
            [Lec] = Driver(Lec, "LEC", "Ferrari"),
            [Pia] = Driver(Pia, "PIA", "McLaren")
        };

        // VER has no grid entry at all -- a pit lane start. Grid has 3 entries, so VER's
        // effective grid position is grid.Count + 1 == 4.
        IReadOnlyList<StartingGridEntry> grid =
        [
            new StartingGridEntry { SessionKey = 100, DriverNumber = Nor, Position = 1, LapDuration = 70.1 },
            new StartingGridEntry { SessionKey = 100, DriverNumber = Lec, Position = 2, LapDuration = 70.4 },
            new StartingGridEntry { SessionKey = 100, DriverNumber = Pia, Position = 3, LapDuration = 70.6 }
        ];

        IReadOnlyList<SessionResultEntry> results =
        [
            new SessionResultEntry { SessionKey = 100, DriverNumber = Nor, Position = 1, Points = 25 },
            new SessionResultEntry { SessionKey = 100, DriverNumber = Ver, Position = 2, Points = 18 },
            new SessionResultEntry { SessionKey = 100, DriverNumber = Lec, Position = 3, Points = 15 },
            new SessionResultEntry { SessionKey = 100, DriverNumber = Pia, Position = 4, Points = 12 }
        ];

        IReadOnlyList<PitStopEntry> pitStops =
        [
            new PitStopEntry { SessionKey = 100, DriverNumber = Nor, StopDuration = 2.50 },
            new PitStopEntry { SessionKey = 100, DriverNumber = Ver, StopDuration = 2.10 },
            new PitStopEntry { SessionKey = 100, DriverNumber = Lec, StopDuration = 2.60 },
            new PitStopEntry { SessionKey = 100, DriverNumber = Pia, StopDuration = 2.70 }
        ];

        var sheet = RaceFactSheet.Build(BaseMeeting(), BaseSession(), results, grid, pitStops, [], directory);

        sheet.Text.Should().Be(
            "2025 Monaco Grand Prix, Monaco, 25 May 2025. Dry. Pole NOR. " +
            "Winner NOR (McLaren) from P1; podium NOR, VER, LEC. " +
            "Biggest gainer VER P4→P2, biggest loser LEC P2→P3. " +
            "0 retirements. Average 1.0 pit stops, fastest stop 2.10s (VER)." +
            "\n\nPoints scorers by team:" +
            "\n- McLaren: 2" +
            "\n- Ferrari: 1" +
            "\n- Red Bull Racing: 1");

        sheet.BiggestGainer.Should().Be("VER P4→P2");
    }

    [Fact]
    public void Build_SameInputsTwice_ProducesTheSameHash()
    {
        var first = BuildBase();
        var second = BuildBase();

        second.Text.Should().Be(first.Text);
        second.ContentHash.Should().Be(first.ContentHash);
    }

    [Fact]
    public void Build_OneFieldChanged_ProducesADifferentHash()
    {
        var baseline = BuildBase();
        var rainedInstead = BuildBase(WetWeather());

        rainedInstead.ContentHash.Should().NotBe(baseline.ContentHash);
    }
}
