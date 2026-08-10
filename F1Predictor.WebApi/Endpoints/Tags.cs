namespace F1Predictor.WebApi.Endpoints;


/// <summary>
/// Provides constant values for endpoint tags used in API documentation and grouping.
/// </summary>
public static class Tags
{
    /// <summary>Ingesting and browsing seasons of race data.</summary>
    public const string Seasons = "Seasons";

    /// <summary>Feature engineering, model training and predictions.</summary>
    public const string Predictions = "Predictions";

    /// <summary>Operational endpoints, exposed in Development only.</summary>
    public const string Admin = "Admin";
}
