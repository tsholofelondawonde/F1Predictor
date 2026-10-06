using F1Predictor.Application.Abstractions.MachineLearning;
using F1Predictor.Application.Features.Predictions.Train;
using F1Predictor.Application.Tests.Fakes;
using F1Predictor.Domain.Predictions;
using F1Predictor.Infrastructure.Database;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace F1Predictor.Application.Tests.Predictions;

public sealed class TrainModelsCommandHandlerTests
{
    private const int DriversPerRace = 5;

    private static TrainModelsCommandHandler Handler(ApplicationDbContext db, FakeModelTrainer trainer) =>
        new(db, trainer, new FakeRacePredictor(), new FakeDateTimeProvider(), NullLogger<TrainModelsCommandHandler>.Instance);

    private static Task SeedSeasonAsync(ApplicationDbContext db, int year, int count, int firstSessionKey) =>
        SeasonSeed.SeedFeatureSeasonAsync(db, year, count, firstSessionKey, DriversPerRace);

    [Fact]
    public async Task Handle_TooFewRaces_FailsWithInsufficientData()
    {
        using var db = InMemoryDb.Create();
        await SeedSeasonAsync(db, 2025, TrainModelsCommandHandler.MinimumRacesToTrain - 1, firstSessionKey: 100);
        var trainer = new FakeModelTrainer();

        var result = await Handler(db, trainer).Handle(new TrainModelsCommand { Year = 2025 }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Training.InsufficientData");
        trainer.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_TenRaces_HoldsOutTheLastAndSplitsTheRestByTime()
    {
        using var db = InMemoryDb.Create();
        await SeedSeasonAsync(db, 2025, 10, firstSessionKey: 100); // keys 100..109
        var trainer = new FakeModelTrainer();

        var result = await Handler(db, trainer).Handle(new TrainModelsCommand { Year = 2025 }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HoldoutRaceName.Should().Be("2025 GP 10");
        result.Value.ValidationRaceNames.Should().Equal("2025 GP 8", "2025 GP 9");

        trainer.Calls.Should().HaveCount(2);
        var data = trainer.Calls[0].Data;
        data.Train.Select(f => f.SessionKey).Distinct().Should().BeEquivalentTo(Enumerable.Range(100, 7));
        data.Validation.Select(f => f.SessionKey).Distinct().Should().BeEquivalentTo([107, 108]);
        data.Train.Concat(data.Validation).Should().NotContain(f => f.SessionKey == 109, "the holdout never reaches the trainer");
    }

    [Fact]
    public async Task Handle_Success_RecordsOneRunPerTargetWithBaselineHoldoutAndNotes()
    {
        using var db = InMemoryDb.Create();
        await SeedSeasonAsync(db, 2025, 10, firstSessionKey: 100);

        var result = await Handler(db, new FakeModelTrainer()).Handle(
            new TrainModelsCommand { Year = 2025, Notes = "  removed pit features  " }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var runs = await db.ModelTrainingRuns.AsNoTracking().ToListAsync();
        runs.Select(r => r.Target).Should().BeEquivalentTo(nameof(PredictionTarget.Podium), nameof(PredictionTarget.PointsFinish));

        var podium = runs.Single(r => r.Target == nameof(PredictionTarget.Podium));
        podium.FromYear.Should().Be(2025);
        podium.Year.Should().Be(2025);
        podium.HoldoutSessionKey.Should().Be(109);
        podium.TrainingRaceCount.Should().Be(7);
        podium.ValidationRaceCount.Should().Be(2);
        podium.ValidationRowCount.Should().Be(2 * DriversPerRace);
        podium.TrainerName.Should().Be("FakeTrainer");
        podium.FeatureNames.Should().Be("GridPosition,QualiGapToPole");
        podium.Notes.Should().Be("removed pit features");

        // Finish == grid in the seed, so ranking by grid is perfect.
        podium.BaselineAuc.Should().Be(1.0);

        // FakeRacePredictor gives every driver the same probability: no separation at all.
        podium.HoldoutAuc.Should().Be(0.5);
        podium.HoldoutLogLoss.Should().NotBeNull();

        result.Value.PodiumEvaluation.RunId.Should().Be(podium.Id);
        result.Value.PodiumEvaluation.BaselineAuc.Should().Be(1.0);
    }

    [Fact]
    public async Task Handle_FromYear_TrainsAcrossSeasonsButHoldsOutFromYear()
    {
        using var db = InMemoryDb.Create();
        // Later season deliberately gets lower session keys: ordering must be by date, not key.
        await SeedSeasonAsync(db, 2024, 6, firstSessionKey: 900);
        await SeedSeasonAsync(db, 2025, 3, firstSessionKey: 100);
        var trainer = new FakeModelTrainer();

        var result = await Handler(db, trainer).Handle(new TrainModelsCommand { Year = 2025, FromYear = 2024 }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FromYear.Should().Be(2024);
        result.Value.HoldoutRaceName.Should().Be("2025 GP 3");
        result.Value.RacesTrainedOn.Should().Be(8);

        var data = trainer.Calls[0].Data;
        data.Validation.Select(f => f.SessionKey).Distinct().Should().BeEquivalentTo([100, 101]);
        data.Train.Select(f => f.SessionKey).Distinct().Should().BeEquivalentTo(Enumerable.Range(900, 6));
    }

    [Fact]
    public async Task Handle_SingleSeasonByDefault_IgnoresEarlierSeasons()
    {
        using var db = InMemoryDb.Create();
        await SeedSeasonAsync(db, 2024, 6, firstSessionKey: 900);
        await SeedSeasonAsync(db, 2025, 6, firstSessionKey: 100);
        var trainer = new FakeModelTrainer();

        var result = await Handler(db, trainer).Handle(new TrainModelsCommand { Year = 2025 }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var data = trainer.Calls[0].Data;
        data.Train.Concat(data.Validation).Should().OnlyContain(f => f.SessionKey < 900);
    }

    [Fact]
    public async Task Handle_NoRacesInYearItself_FailsRatherThanHoldingOutAnEarlierSeason()
    {
        using var db = InMemoryDb.Create();
        await SeedSeasonAsync(db, 2024, 10, firstSessionKey: 900);

        var result = await Handler(db, new FakeModelTrainer()).Handle(
            new TrainModelsCommand { Year = 2025, FromYear = 2024 }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Training.InsufficientData");
        result.Error.Description.Should().Contain("none of them in 2025");
    }

    [Fact]
    public void GridBaselineAuc_FrontOfGridScoresHighest()
    {
        List<DriverRaceFeature> rows =
        [
            new() { GridPosition = 1, Podium = true },
            new() { GridPosition = 2, Podium = false },
            new() { GridPosition = 3, Podium = true },
            new() { GridPosition = 4, Podium = false },
        ];

        // Positives at grid 1 and 3; negatives at 2 and 4. Pairs ranked right: (1,2) (1,4) (3,4) → 3/4.
        TrainModelsCommandHandler.GridBaselineAuc(rows, f => f.Podium).Should().Be(0.75);
    }
}
