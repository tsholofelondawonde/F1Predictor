using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace F1Predictor.Infrastructure.AI;

/// <summary>Reports whether the Ollama daemon answers. Degraded, never Unhealthy — the app runs fine without it.</summary>
internal sealed class OllamaHealthCheck(IHttpClientFactory httpClientFactory) : IHealthCheck
{
    public const string HttpClientName = "Ollama";

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.GetAsync(new Uri("api/tags", UriKind.Relative), cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Ollama is reachable.")
                : HealthCheckResult.Degraded($"Ollama answered {(int)response.StatusCode}.");
        }
        catch (HttpRequestException ex)
        {
            return HealthCheckResult.Degraded("Ollama is not reachable.", ex);
        }
        catch (TaskCanceledException ex)
        {
            return HealthCheckResult.Degraded("Ollama timed out.", ex);
        }
    }
}
