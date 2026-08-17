namespace F1Predictor.Application.Features.Predictions.PreviewNextRace;

/// <param name="Year">Season the race belongs to.</param>
/// <param name="SessionKey">OpenF1 session key of the race.</param>
/// <param name="MeetingKey">OpenF1 meeting key of the weekend.</param>
/// <param name="MeetingName">Race weekend name.</param>
/// <param name="CircuitShortName">Short circuit name.</param>
/// <param name="CountryName">Host country.</param>
/// <param name="DateStart">Scheduled start, in UTC.</param>
/// <param name="HasSprint">Whether the weekend also runs a sprint.</param>
/// <param name="GridConfirmed">
/// True once qualifying has been run and the real starting order is known. While false, every
/// grid position below is projected from recent form and the probabilities should be read as
/// provisional.
/// </param>
/// <param name="Drivers">One row per entered driver, most likely podium first.</param>
public sealed record NextRacePreviewResponse(
    int Year,
    int SessionKey,
    int MeetingKey,
    string MeetingName,
    string CircuitShortName,
    string CountryName,
    DateTimeOffset DateStart,
    bool HasSprint,
    bool GridConfirmed,
    IReadOnlyList<PreviewDriverResponse> Drivers);

/// <param name="DriverNumber">Car number.</param>
/// <param name="FullName">Driver's full name.</param>
/// <param name="NameAcronym">Three-letter abbreviation.</param>
/// <param name="TeamName">Constructor.</param>
/// <param name="TeamColour">Six-digit hex team colour, no leading hash.</param>
/// <param name="GridPosition">Starting position — actual if qualifying has run, otherwise projected.</param>
/// <param name="QualiGapToPole">Gap to pole in seconds, on the same actual-or-projected basis.</param>
/// <param name="HasForm">
/// Whether this driver has finished a Grand Prix this season. Without that there is nothing to
/// project from, so they are placed at the back and their probabilities mean very little.
/// </param>
/// <param name="PodiumProbability">Model's predicted podium probability.</param>
/// <param name="PointsProbability">Model's predicted points-finish probability.</param>
public sealed record PreviewDriverResponse(
    int DriverNumber,
    string FullName,
    string NameAcronym,
    string TeamName,
    string TeamColour,
    float GridPosition,
    float QualiGapToPole,
    bool HasForm,
    float PodiumProbability,
    float PointsProbability);
