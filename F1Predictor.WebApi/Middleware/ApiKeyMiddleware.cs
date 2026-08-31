using F1Predictor.WebApi.Infrastructure;
using System.Security.Cryptography;
using System.Text;

namespace F1Predictor.WebApi.Middleware;

/// <summary>
/// Rejects requests to routes marked with <see cref="RequireApiKeyMetadata"/> unless they carry
/// an <c>X-Api-Key</c> header matching the configured <c>Security:ApiKey</c>. A low bar, not real
/// access control — it filters out naive automated hits against the raw API, not a determined
/// attacker who reads the key out of the frontend bundle it ships in. Fails closed: if no key is
/// configured, every request is rejected rather than let the route go unprotected.
/// </summary>
/// <remarks>
/// Registered after <c>UseCors</c> so a rejection still carries the CORS headers the browser needs
/// to surface it, and before <c>UseRateLimiter</c> so a bad key cannot consume the route's permit.
/// Routes without the metadata pass straight through.
/// </remarks>
internal sealed class ApiKeyMiddleware(IConfiguration configuration) : IMiddleware
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.GetEndpoint()?.Metadata.GetMetadata<RequireApiKeyMetadata>() is null)
        {
            await next(context);
            return;
        }

        var configuredKey = configuration["Security:ApiKey"];

        if (string.IsNullOrEmpty(configuredKey) || !HasValidKey(context, configuredKey))
        {
            await Results.Problem(
                title: "ApiKey.Invalid",
                detail: "A valid X-Api-Key header is required for this endpoint.",
                statusCode: StatusCodes.Status401Unauthorized).ExecuteAsync(context);

            return;
        }

        await next(context);
    }

    private static bool HasValidKey(HttpContext httpContext, string configuredKey)
    {
        if (!httpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedKey))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(providedKey.ToString()),
            Encoding.UTF8.GetBytes(configuredKey));
    }
}
