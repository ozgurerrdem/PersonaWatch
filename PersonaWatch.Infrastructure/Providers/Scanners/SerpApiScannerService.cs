using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PersonaWatch.Application.Abstraction.Services;
using PersonaWatch.Application.Common.Helpers;
using PersonaWatch.Domain.Entities;

namespace PersonaWatch.Infrastructure.Providers.Scanners;

public class SerpApiScannerService : IScanner
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _serpApiKey;

    public string Source => "SerpApi";

    public SerpApiScannerService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _serpApiKey = configuration["SerpApi:ApiKey"] ?? throw new ArgumentNullException("SerpApi:ApiKey is missing");
    }

    public async Task<List<NewsContent>> ScanAsync(string searchKeyword)
    {
        var engines = new[] { "google", "google_news", "google_videos" };

        var tasks = engines.Select(engine => ScanEngineAsync(engine, searchKeyword)).ToArray();

        var allResults = (await Task.WhenAll(tasks))
            .SelectMany(result => result)
            .ToList();

        return allResults;
    }

    private async Task<List<NewsContent>> ScanEngineAsync(string engine, string searchKeyword)
    {
        var results = new List<NewsContent>();
        var client = _httpClientFactory.CreateClient();

        var quotedQuery = $"\"{searchKeyword}\"";
        var searchUrl = $"https://serpapi.com/search.json?engine={engine}&q={Uri.EscapeDataString(quotedQuery)}&hl=tr&gl=tr&num=100&api_key={_serpApiKey}";

        var response = await client.GetStringAsync(searchUrl);
        using var json = JsonDocument.Parse(response);

        // Baz zamanı (UTC) alalım (relative timestamplar için)
        var baseUtc = SerpApiHelperService.GetBaseTimeUtc(json.RootElement);

        var excludedSections = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "related_searches",
            "search_information",
            "pagination",
            "ads",
            "inline_images",
            "menu_links",
            "related_topics",
            "serpapi_pagination"
        };

        foreach (var property in json.RootElement.EnumerateObject())
        {
            if (excludedSections.Contains(property.Name) || property.Value.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var item in property.Value.EnumerateArray())
            {
                var title = item.TryGetProperty("title", out var t) ? t.GetString() :
                            item.TryGetProperty("question", out var q) ? q.GetString() : "";

                var summary = item.TryGetProperty("snippet", out var s) ? s.GetString() : "";
                var url = item.TryGetProperty("link", out var l) ? l.GetString() : "";

                // PublishDate: motor + içerik tipine göre normalize
                DateTime? publishDate = null;

                if (engine.Equals("google_news", StringComparison.OrdinalIgnoreCase))
                {
                    // Örn: "02/04/2025, 08:00 AM, +0000 UTC"
                    var dateStr = item.TryGetProperty("date", out var d) && d.ValueKind == JsonValueKind.String
                        ? d.GetString()
                        : null;
                    publishDate = SerpApiHelperService.ParseGoogleNewsDate(dateStr);
                }
                else if (engine.Equals("google_videos", StringComparison.OrdinalIgnoreCase))
                {
                    publishDate = SerpApiHelperService.ParseGoogleVideosDate(item, baseUtc);

                    // Bazı videolarda ayrıca "date" string'i olabilir; yedek olarak deneyelim
                    if (!publishDate.HasValue && item.TryGetProperty("date", out var vd) && vd.ValueKind == JsonValueKind.String)
                    {
                        publishDate = SerpApiHelperService.ParseDateOrRelative(vd.GetString(), baseUtc);
                    }
                }
                else // "google" (web/organic, vb.)
                {
                    publishDate = SerpApiHelperService.ParseGoogleOrganicDate(item, baseUtc);
                }

                // Fallback: hala yoksa şimdi-UTC
                var finalPublishDate = publishDate ?? DateTime.MinValue;

                // YouTube linkleri hariç tut
                if (!string.IsNullOrEmpty(url) && url.Contains("youtube.com", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(url))
                    continue;

                var normalizedUrl = HelperService.NormalizeUrl(url ?? string.Empty);
                var contentHash = HelperService.ComputeMd5((title ?? string.Empty).Trim() + normalizedUrl);

                results.Add(new NewsContent
                {
                    Id = Guid.NewGuid(),
                    Title = title ?? string.Empty,
                    Summary = summary ?? string.Empty,
                    Url = url ?? string.Empty,
                    Platform = property.Name,              // örn: "organic_results", "news_results", "video_results"
                    PublishDate = finalPublishDate,        // normalize edilmiş UTC
                    CreatedDate = DateTime.UtcNow,
                    CreatedUserName = "system",
                    RecordStatus = 'A',
                    SearchKeyword = searchKeyword,
                    ContentHash = contentHash,
                    Source = Source
                });
            }
        }

        return results;
    }
}