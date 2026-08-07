namespace F1Predictor.Application.Features.Predictions.RebuildFeatures;

/// <param name="RacesWithFeatures">Races that had both grid and result data and so produced rows.</param>
/// <param name="RacesSkipped">Races skipped for missing grid or result data.</param>
/// <param name="FeatureRows">Total driver-race rows in the rebuilt table.</param>
public sealed record RebuildFeaturesResponse(
    int RacesWithFeatures,
    int RacesSkipped,
    int FeatureRows);
