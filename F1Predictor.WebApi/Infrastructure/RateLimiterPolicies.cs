namespace F1Predictor.WebApi.Infrastructure;

/// <summary>Named rate limiter policies, registered in <c>Program.cs</c> via <c>AddRateLimiter</c>.</summary>
internal static class RateLimiterPolicies
{
    /// <summary>1 request per 5 minutes per IP — a full season ingest already takes ~2 minutes itself.</summary>
    public const string Ingest = "Ingest";

    /// <summary>3 requests per minute per IP — rebuild, train and the legacy admin import.</summary>
    public const string Mutating = "Mutating";
}
