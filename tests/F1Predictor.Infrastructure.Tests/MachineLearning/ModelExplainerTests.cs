using F1Predictor.Application.Abstractions.MachineLearning;
using F1Predictor.Domain.Predictions;
using F1Predictor.Infrastructure.MachineLearning;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.ML;
using Xunit;

namespace F1Predictor.Infrastructure.Tests.MachineLearning;

public sealed class ModelExplainerTests
{
    private static readonly string[] ExpectedFeatures =
        ["GridPosition", "QualiGapToPole", "PitStopCount", "AvgPitStopDuration", "Rainfall"];

    private static MlNetRacePredictor PredictorOver(string modelDirectory) =>
        new(Options.Create(new ModelStorageOptions { ModelDirectory = modelDirectory }));

    private static DriverRaceFeature Row(int grid) => new()
    {
        GridPosition = grid, QualiGapToPole = grid * 0.1f, PitStopCount = 2, AvgPitStopDuration = 23, Rainfall = 0
    };

    [Fact]
    public void Explain_LinearModel_ContributionsPlusBiasEqualScore()
    {
        var ml = new MLContext(seed: 42);
        var rows = SyntheticModels.Rows();
        var podiumData = SyntheticModels.DataView(ml, rows, PredictionTarget.Podium);
        var pointsData = SyntheticModels.DataView(ml, rows, PredictionTarget.PointsFinish);
        var (podium, podiumBias) = SyntheticModels.TrainLinear(ml, podiumData);
        var (points, _) = SyntheticModels.TrainLinear(ml, pointsData);
        using var predictor = PredictorOver(SyntheticModels.SaveBoth(ml, podiumData, podium, points));

        var explanation = predictor.Explain(Row(grid: 2));

        explanation.Podium.Contributions.Select(c => c.Feature).Should().Equal(ExpectedFeatures);
        var sum = explanation.Podium.Contributions.Sum(c => c.Contribution) + podiumBias;
        sum.Should().BeApproximately(explanation.Podium.Score, 1e-3f);
    }

    [Fact]
    public void Explain_TreeModel_ReturnsOneContributionPerFeatureAndMatchesPredict()
    {
        var ml = new MLContext(seed: 42);
        var rows = SyntheticModels.Rows();
        var podiumData = SyntheticModels.DataView(ml, rows, PredictionTarget.Podium);
        var pointsData = SyntheticModels.DataView(ml, rows, PredictionTarget.PointsFinish);
        var podium = SyntheticModels.TrainTree(ml, podiumData);
        var points = SyntheticModels.TrainTree(ml, pointsData);
        using var predictor = PredictorOver(SyntheticModels.SaveBoth(ml, podiumData, podium, points));

        var feature = Row(grid: 1);
        var explanation = predictor.Explain(feature);
        var probabilities = predictor.Predict(feature);

        explanation.Podium.Contributions.Select(c => c.Feature).Should().Equal(ExpectedFeatures);
        explanation.PointsFinish.Contributions.Should().HaveCount(5);
        explanation.Podium.Probability.Should().BeApproximately(probabilities.PodiumProbability, 1e-5f);
        explanation.PointsFinish.Probability.Should().BeApproximately(probabilities.PointsProbability, 1e-5f);
    }

    [Fact]
    public void Explain_FrontRowVersusBackRow_GridContributionIsHigherAtTheFront()
    {
        var ml = new MLContext(seed: 42);
        var rows = SyntheticModels.Rows();
        var podiumData = SyntheticModels.DataView(ml, rows, PredictionTarget.Podium);
        var pointsData = SyntheticModels.DataView(ml, rows, PredictionTarget.PointsFinish);
        var (podium, _) = SyntheticModels.TrainLinear(ml, podiumData);
        var (points, _) = SyntheticModels.TrainLinear(ml, pointsData);
        using var predictor = PredictorOver(SyntheticModels.SaveBoth(ml, podiumData, podium, points));

        float GridContribution(int grid) =>
            predictor.Explain(Row(grid)).Podium.Contributions.Single(c => c.Feature == "GridPosition").Contribution;

        GridContribution(1).Should().BeGreaterThan(GridContribution(20));
    }

    [Fact]
    public void Explain_CarriesRawFeatureValues()
    {
        var ml = new MLContext(seed: 42);
        var rows = SyntheticModels.Rows();
        var podiumData = SyntheticModels.DataView(ml, rows, PredictionTarget.Podium);
        var pointsData = SyntheticModels.DataView(ml, rows, PredictionTarget.PointsFinish);
        var (podium, _) = SyntheticModels.TrainLinear(ml, podiumData);
        var (points, _) = SyntheticModels.TrainLinear(ml, pointsData);
        using var predictor = PredictorOver(SyntheticModels.SaveBoth(ml, podiumData, podium, points));

        var explanation = predictor.Explain(Row(grid: 7));

        explanation.Podium.Contributions.Single(c => c.Feature == "GridPosition").Value.Should().Be(7);
        explanation.Podium.Contributions.Single(c => c.Feature == "PitStopCount").Value.Should().Be(2);
    }
}
