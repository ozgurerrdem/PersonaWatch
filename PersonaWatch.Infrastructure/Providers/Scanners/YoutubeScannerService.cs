using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PersonaWatch.Application.Abstraction;
using PersonaWatch.Application.Common.Helpers;

namespace PersonaWatch.Infrastructure.Providers.Scanners;

public class YouTubeScannerService : IScanner
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiKey;

    public string Source => "YouTubeApi";

    public YouTubeScannerService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _apiKey = configuration["YoutubeDataApiV3:ApiKey"] ?? throw new ArgumentNullException(Source + ":ApiKey is missing");
    }

    public async Task<List<Domain.Entities.NewsContent>> ScanAsync(string searchKeyword)
    {
        var results = new List<Domain.Entities.NewsContent>();
        var client = _httpClientFactory.CreateClient();

        var publishedAfter = DateTime.UtcNow.Date.AddDays(-7).ToString("yyyy-MM-dd") + "T00:00:00Z";
        var searchUrl = $"https://www.googleapis.com/youtube/v3/search?part=snippet&q={Uri.EscapeDataString(searchKeyword)}&type=video&maxResults=50&order=date&publishedAfter={publishedAfter}&key={_apiKey}";

        var searchResponse = await client.GetStringAsync(searchUrl);
        using var searchJson = JsonDocument.Parse(searchResponse);

        foreach (var item in searchJson.RootElement.GetProperty("items").EnumerateArray())
        {
            var snippet = item.GetProperty("snippet");
            var title = snippet.GetProperty("title").GetString();
            var description = snippet.GetProperty("description").GetString();
            var videoId = item.GetProperty("id").GetProperty("videoId").GetString();
            var publishedAt = snippet.GetProperty("publishedAt").GetDateTime();

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(videoId))
                continue;

            results.Add(new Domain.Entities.NewsContent
            {
                Id = Guid.NewGuid(),
                Title = title,
                Summary = description ?? string.Empty,
                Url = $"https://www.youtube.com/watch?v={videoId}",
                Platform = "YouTube",
                PublishDate = publishedAt,
                CreatedDate = DateTime.UtcNow,
                CreatedUserName = "system",
                RecordStatus = 'A',
                SearchKeyword = searchKeyword,
                ContentHash = HelperService.ComputeMd5(title + videoId),
                Source = Source
            });
        }

        return results;
    }
}