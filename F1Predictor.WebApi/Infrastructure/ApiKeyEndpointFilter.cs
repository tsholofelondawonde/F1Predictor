using System.Security.Cryptography;
using System.Text;

namespace F1Predictor.WebApi.Infrastructure;

/// <summary>
/// Rejects requests unless they carry an <c>X-Api-Key</c> header matching the configured
/// <c>Security:ApiKey</c>. A low bar, not real access control — it filters out naive
/// automated hits against the raw API, not a determined attacker who reads the key out of
/// the frontend bundle it ships in. Fails closed: if no key is configured, every request
/// is rejected rather than let the route go unprotected.
/// </summary>
internal sealed class ApiKeyEndpointFilter(IConfiguration configuration) : IEndpointFilter
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var configuredKey = configuration["Security:ApiKey"];

        if (string.IsNullOrEmpty(configuredKey) || !HasValidKey(context.HttpContext, configuredKey))
        {
            return Results.Problem(
                title: "ApiKey.Invalid",
                detail: "A valid X-Api-Key header is required for this endpoint.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return await next(context);
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
