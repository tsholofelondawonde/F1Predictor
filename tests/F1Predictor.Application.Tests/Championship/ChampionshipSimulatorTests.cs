using F1Predictor.Domain.Championship;
using FluentAssertions;
using Xunit;

namespace F1Predictor.Application.Tests.Championship;

public sealed class ChampionshipSimulatorTests
{
    private const int Season = 22;

    private static ChampionshipForecast Forecast(int racesDone)
    {
        var standings = DominantSeason.Standings(racesDone);
        var forms = DriverFormModel.Fit(DominantSeason.Outcomes(racesDone));
        var remaining = DominantSeason.Remaining(Season - racesDone, firstKey: racesDone + 1);

        return ChampionshipSimulator.Run(standings, forms, remaining, simulations: 2_000);
    }

    [Fact]
    public void Run_DominantButNotClinchedLeader_GivesRivalsNonZeroOdds()
    {
        var forecast = Forecast(racesDone: 6);

        var leader = forecast.Drivers.Single(d => d.DriverNumber == 1).Odds.TitleProbability;
        var teammate = forecast.Drivers.Single(d => d.DriverNumber == 2).Odds.TitleProbability;

        leader.Should().BeLessThan(1.0);
        teammate.Should().BeGreaterThan(0.0);
        forecast.Drivers.Sum(d => d.Odds.TitleProbability).Should().BeApproximately(1.0, 1e-9);
    }

    [Fact]
    public void Run_DominantOneTwoTeam_GivesRivalConstructorsNonZeroOdds()
    {
        // Three clean one-twos with nineteen rounds left: the leading team is the clear favourite,
        // but three races is not evidence enough to rule the next team out entirely.
        var forecast = Forecast(racesDone: 3);

        var top = forecast.Constructors.Single(c => c.TeamName == "Team 1").Odds.TitleProbability;
        var runnerUp = forecast.Constructors.Single(c => c.TeamName == "Team 2").Odds.TitleProbability;

        top.Should().BeLessThan(1.0);
        runnerUp.Should().BeGreaterThan(0.0);
        forecast.Constructors.Sum(c => c.Odds.TitleProbability).Should().BeApproximately(1.0, 1e-9);
    }
}
