using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace F1Predictor.WebApi.Extensions;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IHostEnvironment env,
        IConfiguration configuration)
    {
        services.AddLogging(logging =>
            logging.AddOpenTelemetry(opts =>
            {
                opts.IncludeFormattedMessage = true;
                opts.IncludeScopes = true;
            })
        );

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: env.ApplicationName,
                    serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0"))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation())
            .WithTracing(tracing => tracing
                .AddSource(env.ApplicationName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation());

        // Prefer explicit config key; fall back to the standard OTEL env var (injected by Aspire at runtime).
        var otlpEndpoint = !string.IsNullOrWhiteSpace(configuration["OpenTelemetry:OtlpEndpoint"])
            ? configuration["OpenTelemetry:OtlpEndpoint"]
            : configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            services.AddOpenTelemetry().UseOtlpExporter();
        }

        return services;
    }
}
