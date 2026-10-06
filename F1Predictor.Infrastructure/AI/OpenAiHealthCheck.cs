using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace F1Predictor.Infrastructure.AI;

/// <summary>Reports whether the configured OpenAI-compatible endpoint answers. Degraded, never
/// Unhealthy — the app runs fine without it, same reasoning as <see cref="OllamaHealthCheck"/>.</summary>
internal sealed class OpenAiHealthCheck(IHttpClientFactory httpClientFactory) : IHealthCheck
{
    public const string HttpClientName = "OpenAi";

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.GetAsync(new Uri("models", UriKind.Relative), cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("The OpenAI-compatible endpoint is reachable.")
                : HealthCheckResult.Degraded($"The OpenAI-compatible endpoint answered {(int)response.StatusCode}.");
        }
        catch (HttpRequestException ex)
        {
            return HealthCheckResult.Degraded("The OpenAI-compatible endpoint is not reachable.", ex);
        }
        catch (TaskCanceledException ex)
        {
            return HealthCheckResult.Degraded("The OpenAI-compatible endpoint timed out.", ex);
        }
    }
}
