using System.Net;
using System.Net.Http.Json;

namespace F1Predictor.OpenF1;

public class OpenF1Client
{
    private readonly HttpClient _httpClient;

    public OpenF1Client(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress ??= new Uri("https://api.openf1.org/v1/");
    }

    public async Task<List<MeetingDto>> GetMeetingsAsync(int year)
    {
        var result = await GetWithRetryAsync<List<MeetingDto>>($"meetings?year={year}");
        return result ?? [];
    }

    public async Task<List<SessionDto>> GetSessionsAsync(int meetingKey)
    {
        var result = await GetWithRetryAsync<List<SessionDto>>($"sessions?meeting_key={meetingKey}");
        return result ?? [];
    }

    public async Task<List<StartingGridDto>> GetStartingGridAsync(int sessionKey)
    {
        var result = await GetWithRetryAsync<List<StartingGridDto>>($"starting_grid?session_key={sessionKey}");
        return result ?? [];
    }

    public async Task<List<SessionResultDto>> GetSessionResultAsync(int sessionKey)
    {
        var result = await GetWithRetryAsync<List<SessionResultDto>>($"session_result?session_key={sessionKey}");
        return result ?? [];
    }

    public async Task<List<PitDto>> GetPitStopsAsync(int sessionKey)
    {
        var result = await GetWithRetryAsync<List<PitDto>>($"pit?session_key={sessionKey}");
        return result ?? [];
    }

    public async Task<List<WeatherDto>> GetWeatherAsync(int sessionKey)
    {
        var result = await GetWithRetryAsync<List<WeatherDto>>($"weather?session_key={sessionKey}");
        return result ?? [];
    }

    private async Task<T?> GetWithRetryAsync<T>(string requestUri)
    {
        const int maxAttempts = 8;
        var delay = TimeSpan.FromSeconds(2);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var response = await _httpClient.GetAsync(requestUri);

            if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt < maxAttempts)
            {
                await Task.Delay(delay);
                delay *= 2;
                continue;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }

        throw new HttpRequestException($"Exceeded retry attempts for {requestUri} due to repeated 429 responses.");
    }
}
