using F1Predictor.Application.Abstractions.Messaging;

namespace F1Predictor.Application.Features.Predictions.Train;

/// <summary>
/// Trains and saves both classifiers on a season's feature rows, excluding the most recent
/// race so it stays available as an honest holdout.
/// </summary>
public sealed class TrainModelsCommand : ICommand<TrainModelsResponse>
{
    public int Year { get; set; }
}
