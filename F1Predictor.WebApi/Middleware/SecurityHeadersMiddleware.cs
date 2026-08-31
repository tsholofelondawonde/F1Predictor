namespace F1Predictor.WebApi.Middleware;

/// <summary>
/// Sets baseline security response headers on every request. The Content-Security-Policy is
/// skipped under <c>/scalar</c> and <c>/openapi</c> — the only HTML/script surface this API
/// serves — so the Scalar reference UI keeps working; every other route is a pure JSON API
/// that needs none of it.
/// </summary>
public sealed class SecurityHeadersMiddleware : IMiddleware
{
    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            if (!IsScalarOrOpenApiPath(context.Request.Path))
            {
                headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
            }

            return Task.CompletedTask;
        });

        return next(context);
    }

    private static bool IsScalarOrOpenApiPath(PathString path) =>
        path.StartsWithSegments("/scalar", StringComparison.Ordinal) ||
        path.StartsWithSegments("/openapi", StringComparison.Ordinal);
}
