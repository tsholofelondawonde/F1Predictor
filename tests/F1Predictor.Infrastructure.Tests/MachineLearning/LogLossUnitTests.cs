using F1Predictor.Application.Features.Predictions.Evaluation;
using FluentAssertions;
using Microsoft.ML;
using Xunit;

namespace F1Predictor.Infrastructure.Tests.MachineLearning;

/// <summary>
/// The training-run log stores ML.NET's validation log loss next to a hand-computed holdout log
/// loss. They are only comparable if both are in the same unit, so pin that down against
/// ML.NET's own evaluator rather than trusting the docs.
/// </summary>
public sealed class LogLossUnitTests
{
    private sealed class Scored
    {
        public bool Label { get; set; }
        public float Score { get; set; }
        public float Probability { get; set; }
        public bool PredictedLabel { get; set; }
    }

    [Fact]
    public void BinaryMetricsLogLoss_MatchesMlNetEvaluator()
    {
        (float Probability, bool Label)[] rows =
            [(0.9f, true), (0.7f, true), (0.4f, true), (0.6f, false), (0.2f, false), (0.1f, false), (0.3f, false)];

        var mlContext = new MLContext(seed: 1);
        var data = mlContext.Data.LoadFromEnumerable(rows.Select(r => new Scored
        {
            Label = r.Label,
            Probability = r.Probability,
            Score = r.Probability - 0.5f,
            PredictedLabel = r.Probability >= 0.5f
        }));

        var mlNet = mlContext.BinaryClassification.Evaluate(data);
        var ours = BinaryMetrics.LogLoss([.. rows.Select(r => ((double)r.Probability, r.Label))]);

        ours.Should().BeApproximately(mlNet.LogLoss, 1e-5);
    }
}
