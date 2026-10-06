using F1Predictor.Application.Features.Predictions.GetHoldout;
using F1Predictor.Application.Features.Predictions.GetTrainingRuns;
using F1Predictor.Application.Tests.Fakes;
using F1Predictor.Domain.Predictions;
using F1Predictor.Infrastructure.Database;
using FluentAssertions;
using Xunit;

namespace F1Predictor.Application.Tests.Predictions;

public sealed class TrainingRunsAndHoldoutTests
{
    private static readonly DateTimeOffset Earlier = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = Earlier.AddDays(1);

    private static ModelTrainingRun Run(string target, DateTimeOffset trainedAt, int holdoutSessionKey, double auc = 0.8, double? baseline = 0.7) => new()
    {
        TrainedAt = trainedAt,
        Target = target,
        FromYear = 2025,
        Year = 2025,
        TrainerName = "FastTreeBinary",
        FeatureNames = "GridPosition,QualiGapToPole",
        ValidationAuc = auc,
        BaselineAuc = baseline,
        HoldoutSessionKey = holdoutSessionKey
    };

    private static async Task AddRunsAsync(ApplicationDbContext db, params ModelTrainingRun[] runs)
    {
        db.ModelTrainingRuns.AddRange(runs);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetTrainingRuns_NewestFirst_WithFeatureListAndLiftOverBaseline()
    {
        using var db = InMemoryDb.Create();
        await AddRunsAsync(db,
            Run("Podium", Earlier, 1, auc: 0.70, baseline: 0.72),
            Run("Podium", Later, 1, auc: 0.80, baseline: 0.72));

        var result = await new GetTrainingRunsQueryHandler(db).Handle(new GetTrainingRunsQuery(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Runs.Select(r => r.TrainedAt).Should().Equal(Later, Earlier);
        result.Value.Runs[0].FeatureNames.Should().Equal("GridPosition", "QualiGapToPole");
        result.Value.Runs[0].AucOverBaseline.Should().BeApproximately(0.08, 1e-9);
        result.Value.Runs[1].AucOverBaseline.Should().BeApproximately(-0.02, 1e-9, "a model can lose to the grid, and that must show");
    }

    [Fact]
    public async Task GetTrainingRuns_TargetFilter_IsCaseInsensitive()
    {
        using var db = InMemoryDb.Create();
        await AddRunsAsync(db, Run("Podium", Earlier, 1), Run("PointsFinish", Earlier, 1));

        var result = await new GetTrainingRunsQueryHandler(db).Handle(new GetTrainingRunsQuery("pointsfinish"), CancellationToken.None);

        result.Value.Runs.Should().ContainSingle().Which.Target.Should().Be("PointsFinish");
    }

    [Fact]
    public async Task GetTrainingRuns_UnknownTarget_Fails()
    {
        using var db = InMemoryDb.Create();

        var result = await new GetTrainingRunsQueryHandler(db).Handle(new GetTrainingRunsQuery("fastest-lap"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Training.UnknownTarget");
    }

    [Fact]
    public async Task GetTrainingRuns_NoBaseline_LiftIsNull()
    {
        using var db = InMemoryDb.Create();
        await AddRunsAsync(db, Run("Podium", Earlier, 1, baseline: null));

        var result = await new GetTrainingRunsQueryHandler(db).Handle(new GetTrainingRunsQuery(null), CancellationToken.None);

        result.Value.Runs[0].AucOverBaseline.Should().BeNull();
    }

    [Fact]
    public async Task GetHoldout_LatestRunHeldOutThisRace_NoWarning()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedFeatureSeasonAsync(db, 2025, 3, firstSessionKey: 100); // holdout = 102
        await AddRunsAsync(db, Run("Podium", Earlier, holdoutSessionKey: 55), Run("Podium", Later, holdoutSessionKey: 102));

        var result = await new GetHoldoutPredictionsQueryHandler(db, new FakeRacePredictor())
            .Handle(new GetHoldoutPredictionsQuery(2025), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SessionKey.Should().Be(102);
        result.Value.ModelWarning.Should().BeNull();
    }

    [Fact]
    public async Task GetHoldout_LatestRunHeldOutADifferentRace_Warns()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedFeatureSeasonAsync(db, 2025, 3, firstSessionKey: 100);
        // An older run matched, but the newest one — the models on disk — did not.
        await AddRunsAsync(db, Run("Podium", Earlier, holdoutSessionKey: 102), Run("Podium", Later, holdoutSessionKey: 55));

        var result = await new GetHoldoutPredictionsQueryHandler(db, new FakeRacePredictor())
            .Handle(new GetHoldoutPredictionsQuery(2025), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ModelWarning.Should().Contain("2025 GP 3").And.Contain("year=2025");
        result.Value.Drivers.Should().NotBeEmpty("the warning accompanies the predictions, it doesn't replace them");
    }

    [Fact]
    public async Task GetHoldout_NoRunsRecorded_Warns()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedFeatureSeasonAsync(db, 2025, 3, firstSessionKey: 100);

        var result = await new GetHoldoutPredictionsQueryHandler(db, new FakeRacePredictor())
            .Handle(new GetHoldoutPredictionsQuery(2025), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ModelWarning.Should().Contain("No training run is recorded");
    }
}
