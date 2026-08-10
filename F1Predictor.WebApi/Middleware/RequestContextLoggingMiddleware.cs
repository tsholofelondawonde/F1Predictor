using Microsoft.Extensions.Primitives;
using System.Diagnostics;

namespace F1Predictor.WebApi.Middleware;

public class RequestContextLoggingMiddleware(ILogger<RequestContextLoggingMiddleware> logger)
    : IMiddleware
{
    private const string CorrelationIdHeaderName = "Correlation-Id";

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var correlationId = GetCorrelationId(context);

        // Enrich the active OTel span so correlation ID appears in traces.
        Activity.Current?.SetTag("correlation.id", correlationId);

        // Enrich structured logs via ILogger scope — picked up by the OTel logging bridge.
        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next.Invoke(context);
        }
    }

    private static string GetCorrelationId(HttpContext context)
    {
        context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out StringValues correlationId);
        return correlationId.FirstOrDefault() ?? context.TraceIdentifier;
    }
}
