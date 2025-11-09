using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PersonaWatch.Application.DTOs.Providers.Apify;

namespace PersonaWatch.Infrastructure.Providers.Apify;

public class ApifyClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiToken;

    public ApifyClient(IConfiguration config, HttpClient httpClient)
    {
        _httpClient = httpClient;
        _apiToken = config["Apify:ApiKey"] ?? throw new Exception("Apify token not configured.");
    }

    public async Task<string> StartActorAsync(string actorId, object input)
    {
        var uri = $"https://api.apify.com/v2/acts/{actorId}/runs?token={_apiToken}";
        var response = await _httpClient.PostAsJsonAsync(uri, new { input });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<ApifyRunResponse>();
        return json?.Data?.Id ?? throw new Exception("Run ID boş.");
    }

    public async Task<string> StartActorRawAsync(string actorId, object rawInput)
    {
        var uri = $"https://api.apify.com/v2/acts/{actorId}/runs?token={_apiToken}";
        var response = await _httpClient.PostAsJsonAsync(uri, rawInput);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<ApifyRunResponse>();
        return json?.Data?.Id ?? throw new Exception("Run ID boş.");
    }

    public async Task<string?> GetRunStatusAsync(string runId)
    {
        var uri = $"https://api.apify.com/v2/actor-runs/{runId}?token={_apiToken}";
        var res = await _httpClient.GetFromJsonAsync<ApifyRunResponse>(uri);
        return res?.Data?.Status;
    }

    public async Task<string?> GetDatasetIdAsync(string runId)
    {
        var uri = $"https://api.apify.com/v2/actor-runs/{runId}?token={_apiToken}";
        var res = await _httpClient.GetFromJsonAsync<ApifyRunResponse>(uri);
        return res?.Data?.DefaultDatasetId;
    }

    public async Task<List<T>> GetDatasetItemsAsync<T>(string datasetId, CancellationToken ct = default)
    {
        var uri = $"https://api.apify.com/v2/datasets/{datasetId}/items?token={_apiToken}&format=json&clean=true";

        const int maxAttempts = 5;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var json = await _httpClient.GetStringAsync(uri, ct);
                if (string.IsNullOrWhiteSpace(json))
                {
                    if (attempt == maxAttempts) return new List<T>();
                }
                else
                {
                    var items = JsonSerializer.Deserialize<List<T>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return items ?? new List<T>();
                }
            }
            catch (HttpRequestException) when (attempt < maxAttempts)
            {
                // geçici hata: tekrar dene
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested && attempt < maxAttempts)
            {
                // timeout/iptal değilse tekrar dene
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt * attempt), ct); // exponential backoff
        }

        return new List<T>();
    }
}
