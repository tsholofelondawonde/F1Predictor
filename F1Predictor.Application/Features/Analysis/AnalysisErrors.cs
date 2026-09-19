using SharedKernel;

namespace F1Predictor.Application.Features.Analysis;

internal static class AnalysisErrors
{
    public static readonly Error AiUnavailable = Error.Problem(
        "Analysis.AiUnavailable",
        "No AI provider is configured (Ai:Provider is None), so narratives and chat are unavailable.",
        "AI analysis is not enabled on this deployment.");

    public static readonly Error ModelRefused = Error.Problem(
        "Analysis.ModelRefused",
        "The language model returned an empty response.",
        "The AI did not produce a preview. Try again.");

    public static readonly Error ProviderFailed = Error.Problem(
        "Analysis.ProviderFailed",
        "The language model provider threw while writing the preview; the exception is in the log.",
        "The AI provider failed while writing the preview. Try again.");

    public static Error PreviewNotFound(int sessionKey) => Error.NotFound(
        "Analysis.PreviewNotFound",
        $"No preview narrative has been generated for session {sessionKey}.",
        "No preview has been generated for this race yet.");

    public static Error NotNextRace(int sessionKey) => Error.Problem(
        "Analysis.NotNextRace",
        $"Session {sessionKey} is not the next Grand Prix, so there is no grid or projection to preview from.",
        "Previews can only be generated for the next Grand Prix.");

    public static Error RaceNotFound(int sessionKey) => Error.NotFound(
        "Analysis.RaceNotFound",
        $"No race session {sessionKey} has been ingested.",
        "That race is not in the database.");

    public static Error DriverNotEntered(int driverNumber) => Error.NotFound(
        "Analysis.DriverNotEntered",
        $"Car {driverNumber} is not on the entry list for the next Grand Prix.",
        "That driver is not entered in the next race.");

    public static Error DriverNotInRace(int sessionKey, int driverNumber) => Error.NotFound(
        "Analysis.DriverNotInRace",
        $"Car {driverNumber} has no recorded result for session {sessionKey}.",
        "That driver did not take part in this race.");
}
