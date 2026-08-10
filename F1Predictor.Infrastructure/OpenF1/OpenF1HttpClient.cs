using System.Net.Http.Json;
using System.Text.Json;
using F1Predictor.Application.Abstractions.OpenF1;

namespace F1Predictor.Infrastructure.OpenF1;

/// <summary>
/// Typed client over the OpenF1 REST API.
/// </summary>
/// <remarks>
/// Rate limiting and retries are handled by the message handlers configured on the injected
/// <see cref="HttpClient"/> — a politeness delay plus the standard resilience pipeline — so
/// nothing here waits or retries by hand.
/// </remarks>
internal sealed class OpenF1HttpClient(HttpClient httpClient) : IOpenF1Client
{
    /// <summary>
    /// OpenF1 field names are snake_case throughout, which maps cleanly onto the PascalCase
    /// model properties without a per-property attribute on each one.
    /// </summary>
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public Task<IReadOnlyList<OpenF1Meeting>> GetMeetingsAsync(int year, CancellationToken cancellationToken) =>
        GetAsync<OpenF1Meeting>($"meetings?year={year}", cancellationToken);

    public Task<IReadOnlyList<OpenF1Session>> GetSessionsAsync(int meetingKey, CancellationToken cancellationToken) =>
        GetAsync<OpenF1Session>($"sessions?meeting_key={meetingKey}", cancellationToken);

    public Task<IReadOnlyList<OpenF1StartingGrid>> GetStartingGridAsync(int sessionKey, CancellationToken cancellationToken) =>
        GetAsync<OpenF1StartingGrid>($"starting_grid?session_key={sessionKey}", cancellationToken);

    public Task<IReadOnlyList<OpenF1SessionResult>> GetSessionResultAsync(int sessionKey, CancellationToken cancellationToken) =>
        GetAsync<OpenF1SessionResult>($"session_result?session_key={sessionKey}", cancellationToken);

    public Task<IReadOnlyList<OpenF1Pit>> GetPitStopsAsync(int sessionKey, CancellationToken cancellationToken) =>
        GetAsync<OpenF1Pit>($"pit?session_key={sessionKey}", cancellationToken);

    public Task<IReadOnlyList<OpenF1Weather>> GetWeatherAsync(int sessionKey, CancellationToken cancellationToken) =>
        GetAsync<OpenF1Weather>($"weather?session_key={sessionKey}", cancellationToken);

    private async Task<IReadOnlyList<T>> GetAsync<T>(string requestUri, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(new Uri(requestUri, UriKind.Relative), cancellationToken);

        // OpenF1 is inconsistent about "no rows": most list endpoints return 200 with an
        // empty array, but some (e.g. pit) return 404 with {"detail":"No results found."}
        // for a session that simply has none. Both mean the same thing to callers here.
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return [];
        }

        response.EnsureSuccessStatusCode();

        var payload = await response.Content
            .ReadFromJsonAsync<List<T>>(SerializerOptions, cancellationToken);

        return payload ?? [];
    }
}
