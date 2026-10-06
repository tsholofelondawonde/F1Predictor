using F1Predictor.Application.Abstractions.Messaging;

namespace F1Predictor.Application.Features.Predictions.Train;

/// <summary>
/// Trains and saves both classifiers on the feature rows of seasons <see cref="FromYear"/>
/// through <see cref="Year"/>, excluding the most recent race of <see cref="Year"/> so it stays
/// available as an honest holdout. Every run is recorded in <c>ModelTrainingRuns</c>.
/// </summary>
public sealed class TrainModelsCommand : ICommand<TrainModelsResponse>
{
    /// <summary>Last season to train on; the holdout race comes from it.</summary>
    public int Year { get; set; }

    /// <summary>First season to train on. Null means <see cref="Year"/> alone.</summary>
    public int? FromYear { get; set; }

    /// <summary>What this experiment changed, stored with the run, e.g. "removed pit features".</summary>
    public string? Notes { get; set; }
}
