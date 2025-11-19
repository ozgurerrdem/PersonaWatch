using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PersonaWatch.Application.Abstraction;
using PersonaWatch.Application.Common.Helpers;
using PersonaWatch.Application.DTOs.Providers.Filmot;
using PersonaWatch.Application.DTOs.Providers.RapidApi.Filmot;

namespace PersonaWatch.Infrastructure.Providers.Scanners.RapidApi;

public class FilmotRapidApiScannerService : IScanner
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly string? _apiKey;
    private readonly string _host;
    private readonly string _baseUrl;

    public FilmotRapidApiScannerService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;

        _apiKey = _configuration["FilmotRapidApi:ApiKey"];
        _host = _configuration["FilmotRapidApi:Host"]
                ?? "filmot-tube-metadata-archive.p.rapidapi.com";
        _baseUrl = _configuration["FilmotRapidApi:BaseUrl"]
                   ?? "https://filmot-tube-metadata-archive.p.rapidapi.com";
    }

    public string Source => "Filmot";

    public async Task<List<Domain.Entities.NewsContent>> ScanAsync(string searchKeyword)
    {
        var results = new List<Domain.Entities.NewsContent>();

        if (string.IsNullOrWhiteSpace(searchKeyword))
            return results;

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return results;
        }

        var client = _httpClientFactory.CreateClient();

        var query = Uri.EscapeDataString($"\"{searchKeyword.Trim()}\"");

        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddMonths(-2);

        var url = $"{_baseUrl}/getsearchsubtitles" +
                $"?query={query}" +
                $"&startDate={startDate:yyyy-MM-dd}" +
                $"&endDate={endDate:yyyy-MM-dd}" +
                "&sortField=uploaddate" +
                "&sortOrder=desc";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("x-rapidapi-key", _apiKey);
        request.Headers.Add("x-rapidapi-host", _host);

        try
        {
            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var apiResponse =
                JsonSerializer.Deserialize<FilmotRapidApiSearchResponse>(body, options);

            if (apiResponse?.Result == null || apiResponse.Result.Count == 0)
            {
                return results;
            }

            foreach (var video in apiResponse.Result)
            {
                if (video.Hits == null || video.Hits.Count == 0)
                    continue;

                var videoId = video.Id ?? string.Empty;

                var titleFromVideo = video.Title;
                var channelName = string.IsNullOrWhiteSpace(video.Channelname)
                    ? "Unknown Channel"
                    : video.Channelname;

                var publishDate = ParseUploadDate(video.Uploaddate);

                var viewCount = (int)video.Viewcount;
                var likeCount = (int)video.Likecount;

                foreach (var hit in video.Hits)
                {
                    var fullText = $"{hit.CtxBefore?.Trim()} {hit.Token?.Trim()} {hit.CtxAfter?.Trim()}".Trim();
                    var urlWithTimestamp = $"https://www.youtube.com/watch?v={videoId}&t={(int)hit.Start}s";
                    var baseUrl = $"https://www.youtube.com/watch?v={videoId}";
                    var normalizedUrl = HelperService.NormalizeUrl(baseUrl);
                    var contentHash = HelperService.ComputeMd5((hit.Token ?? string.Empty) + normalizedUrl);

                    var title = !string.IsNullOrWhiteSpace(titleFromVideo)
                        ? titleFromVideo
                        : (hit.Token ?? searchKeyword);

                    results.Add(new Domain.Entities.NewsContent
                    {
                        Id = Guid.NewGuid(),
                        Title = title,
                        Summary = fullText,
                        Url = urlWithTimestamp,
                        Platform = "YouTube",
                        PublishDate = publishDate,
                        CreatedDate = DateTime.UtcNow,
                        CreatedUserName = "system",
                        RecordStatus = 'A',
                        SearchKeyword = searchKeyword,
                        ContentHash = contentHash,
                        Source = Source,
                        Publisher = channelName,
                        ViewCount = viewCount,
                        LikeCount = likeCount
                    });
                }
            }

            return results;
        }
        catch (Exception)
        {
            return results;
        }
    }

    private static DateTime ParseUploadDate(string? uploadDate)
    {
        if (string.IsNullOrWhiteSpace(uploadDate))
            return DateTime.UtcNow;

        if (DateTime.TryParseExact(
                uploadDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var dt))
        {
            return dt;
        }

        return DateTime.UtcNow;
    }
}