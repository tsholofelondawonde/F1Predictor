using F1Predictor.Application.Features.Predictions.Evaluation;
using FluentAssertions;
using Xunit;

namespace F1Predictor.Application.Tests.Predictions;

public sealed class BinaryMetricsTests
{
    [Fact]
    public void Auc_PerfectRanking_IsOne()
    {
        var auc = BinaryMetrics.Auc([(0.9, true), (0.8, true), (0.2, false), (0.1, false)]);

        auc.Should().Be(1.0);
    }

    [Fact]
    public void Auc_ReversedRanking_IsZero()
    {
        var auc = BinaryMetrics.Auc([(0.1, true), (0.2, true), (0.8, false), (0.9, false)]);

        auc.Should().Be(0.0);
    }

    [Fact]
    public void Auc_KnownMixedCase_CountsCorrectlyOrderedPairs()
    {
        // Positives 0.8, 0.4; negatives 0.6, 0.2. Pairs: (0.8>0.6) (0.8>0.2) (0.4<0.6) (0.4>0.2) → 3/4.
        var auc = BinaryMetrics.Auc([(0.8, true), (0.4, true), (0.6, false), (0.2, false)]);

        auc.Should().Be(0.75);
    }

    [Fact]
    public void Auc_TiedScoresAcrossClasses_CountHalf()
    {
        // Every pair tied: a model that cannot separate anything scores exactly chance.
        var auc = BinaryMetrics.Auc([(0.5, true), (0.5, false), (0.5, true), (0.5, false)]);

        auc.Should().Be(0.5);
    }

    [Fact]
    public void Auc_SingleClass_IsUndefined()
    {
        BinaryMetrics.Auc([(0.9, true), (0.1, true)]).Should().BeNull();
        BinaryMetrics.Auc([(0.9, false), (0.1, false)]).Should().BeNull();
        BinaryMetrics.Auc([]).Should().BeNull();
    }

    [Fact]
    public void LogLoss_ConfidentAndRight_IsNearZero_ConfidentAndWrong_IsLarge()
    {
        var right = BinaryMetrics.LogLoss([(0.99, true), (0.01, false)]);
        var wrong = BinaryMetrics.LogLoss([(0.01, true), (0.99, false)]);

        right.Should().BeLessThan(0.02);
        wrong.Should().BeGreaterThan(6);
    }

    [Fact]
    public void LogLoss_CoinFlip_IsOneBit()
    {
        // Measured in bits (log base 2), the unit ML.NET reports, so a 50/50 guess costs exactly 1.
        var logLoss = BinaryMetrics.LogLoss([(0.5, true), (0.5, false)]);

        logLoss.Should().BeApproximately(1.0, 1e-12);
    }

    [Fact]
    public void LogLoss_ProbabilityOfExactlyZeroOnAPositive_IsFiniteNotInfinity()
    {
        var logLoss = BinaryMetrics.LogLoss([(0.0, true)]);

        logLoss.Should().NotBeNull();
        double.IsFinite(logLoss.Value).Should().BeTrue();
    }

    [Fact]
    public void LogLoss_Empty_IsUndefined()
    {
        BinaryMetrics.LogLoss([]).Should().BeNull();
    }
}
