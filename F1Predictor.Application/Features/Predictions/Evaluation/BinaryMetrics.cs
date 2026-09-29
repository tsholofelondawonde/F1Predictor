namespace F1Predictor.Application.Features.Predictions.Evaluation;

/// <summary>
/// Hand-rolled binary-classification metrics for the places ML.NET's evaluator can't be used:
/// scoring the grid-order baseline (not a model at all) and scoring the held-out race through
/// the saved model files.
/// </summary>
/// <remarks>
/// Both return null rather than throwing when the metric is undefined — a single held-out race
/// can easily carry only one class once a podium finisher is disqualified, and "undefined" is
/// the honest answer there, not an error.
/// </remarks>
internal static class BinaryMetrics
{
    /// <summary>Clamp that keeps a probability of exactly 0 or 1 from producing an infinite log loss.</summary>
    private const double Epsilon = 1e-15;

    /// <summary>
    /// Area under the ROC curve: the probability that a randomly chosen positive scores above a
    /// randomly chosen negative, with ties counted as half. Null when either class is absent.
    /// </summary>
    /// <remarks>
    /// Computed from average ranks (the Mann–Whitney U statistic) rather than by walking
    /// thresholds, which is what makes the tie handling exact.
    /// </remarks>
    public static double? Auc(IReadOnlyList<(double Score, bool Label)> scored)
    {
        ArgumentNullException.ThrowIfNull(scored);

        var positives = scored.Count(s => s.Label);
        var negatives = scored.Count - positives;

        if (positives == 0 || negatives == 0)
        {
            return null;
        }

        var positiveRankSum = 0.0;
        var ranksUsed = 0;

        foreach (var tieGroup in scored.GroupBy(s => s.Score).OrderBy(g => g.Key))
        {
            var size = tieGroup.Count();

            // Ranks are 1-based; every member of a tie group shares the group's average rank.
            var averageRank = ranksUsed + (size + 1) / 2.0;
            positiveRankSum += averageRank * tieGroup.Count(s => s.Label);
            ranksUsed += size;
        }

        var u = positiveRankSum - positives * (positives + 1) / 2.0;
        return u / ((double)positives * negatives);
    }

    /// <summary>
    /// Mean log loss in bits (log base 2) — the same unit ML.NET's <c>LogLoss</c> reports, so the
    /// held-out number can be read against the validation one. A 50/50 guess costs exactly 1.
    /// Null for an empty set.
    /// </summary>
    public static double? LogLoss(IReadOnlyList<(double Probability, bool Label)> predicted)
    {
        ArgumentNullException.ThrowIfNull(predicted);

        if (predicted.Count == 0)
        {
            return null;
        }

        return predicted.Average(p =>
        {
            var probability = Math.Clamp(p.Probability, Epsilon, 1 - Epsilon);
            return -Math.Log2(p.Label ? probability : 1 - probability);
        });
    }
}
