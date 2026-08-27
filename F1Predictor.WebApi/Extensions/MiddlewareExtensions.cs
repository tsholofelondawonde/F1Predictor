using F1Predictor.WebApi.Middleware;

namespace F1Predictor.WebApi.Extensions;


/// <summary>
/// Provides extension methods for registering custom middleware in the application pipeline.
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Adds the <see cref="RequestContextLoggingMiddleware"/> to the application's request pipeline.
    /// This middleware enriches logs and traces with a correlation ID for each request.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> for chaining.</returns>
    public static IApplicationBuilder UseRequestContextLogging(this IApplicationBuilder app)
    {
        app.UseMiddleware<RequestContextLoggingMiddleware>();

        return app;
    }

    /// <summary>
    /// Adds the <see cref="SecurityHeadersMiddleware"/> to the application's request pipeline.
    /// Sets baseline security response headers on every request.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> for chaining.</returns>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        app.UseMiddleware<SecurityHeadersMiddleware>();

        return app;
    }

    /// <summary>
    /// Adds the <see cref="ApiKeyMiddleware"/> to the application's request pipeline, enforcing
    /// <c>X-Api-Key</c> on the routes marked by <c>RequireApiKey()</c>.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> for chaining.</returns>
    /// <remarks>
    /// Must sit after <c>UseRouting</c> (it reads endpoint metadata) and after <c>UseCors</c> (so a
    /// 401 carries the headers the browser needs to read it), but before <c>UseRateLimiter</c>, so
    /// an unauthenticated request cannot spend the route's rate limit permit.
    /// </remarks>
    public static IApplicationBuilder UseApiKeyValidation(this IApplicationBuilder app)
    {
        app.UseMiddleware<ApiKeyMiddleware>();

        return app;
    }
}
