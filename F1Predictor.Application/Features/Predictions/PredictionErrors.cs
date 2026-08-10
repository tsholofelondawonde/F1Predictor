using SharedKernel;

namespace F1Predictor.Application.Features.Predictions;

internal static class PredictionErrors
{
    public static readonly Error ModelsNotTrained = Error.Problem(
        "Prediction.ModelsNotTrained",
        "No trained models are available. Train the models before requesting predictions.",
        "The prediction models have not been trained yet.");

    public static Error RaceNotFound(int sessionKey) => Error.NotFound(
        "Prediction.RaceNotFound",
        $"No feature rows exist for session {sessionKey}.",
        "That race has no data to predict from. Ingest the season and rebuild the features first.");
}
