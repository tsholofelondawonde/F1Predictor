namespace F1Predictor.WebApi.Infrastructure;

/// <summary>
/// Marks a route as requiring a valid <c>X-Api-Key</c> header. Applied by
/// <c>RequireApiKey()</c> and read by <see cref="Middleware.ApiKeyMiddleware"/>.
/// </summary>
/// <remarks>
/// This is endpoint metadata rather than an endpoint filter because the check has to run
/// <em>before</em> <c>UseRateLimiter</c>. Endpoint filters run inside endpoint execution,
/// which is after every middleware — so as a filter the key check could only ever run after
/// the limiter had already charged the request a permit, letting an unauthenticated caller
/// spend the ingest budget (1 per 5 minutes) that a legitimate one needs.
/// </remarks>
internal sealed class RequireApiKeyMetadata
{
    /// <summary>The marker carries no state, so every route shares one instance.</summary>
    public static RequireApiKeyMetadata Instance { get; } = new();
}
