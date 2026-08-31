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

    public static Error NoUpcomingRace(int year) => Error.NotFound(
        "Prediction.NoUpcomingRace",
        $"Every Grand Prix of {year} that has been ingested already has results.",
        "There is no upcoming race to preview. Re-ingest the season to pick up the rest of the calendar.");

    public static Error NoEntryList(string meetingName) => Error.NotFound(
        "Prediction.NoEntryList",
        $"No drivers are recorded for the {meetingName} race session.",
        "The entry list for that race has not been ingested yet.");
}
