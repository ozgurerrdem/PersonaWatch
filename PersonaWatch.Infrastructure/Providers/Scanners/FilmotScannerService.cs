using System.Text.Json;
using System.Text.RegularExpressions;
using PersonaWatch.Application.Abstraction.Services;
using PersonaWatch.Application.Common.Helpers;
using PersonaWatch.Application.DTOs.Providers.Filmot;
using PersonaWatch.Domain.Entities;

namespace PersonaWatch.Infrastructure.Providers.Scanners;

public class FilmotScannerService : IScanner
{
    private readonly IHttpClientFactory _httpClientFactory;

    public string Source => "Filmot";

    public FilmotScannerService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<NewsContent>> ScanAsync(string searchKeyword)
    {
        var results = new List<NewsContent>();

        if (string.IsNullOrWhiteSpace(searchKeyword))
            return results;

        var encoded = Uri.EscapeDataString(Regex.Replace(searchKeyword.Trim(), @"\s+", "+"));

        var url = $"https://filmot.com/search/%22{encoded}%22/1?sortField=uploaddate&sortOrder=desc&gridView=1&";

        var client = _httpClientFactory.CreateClient();

        string html;
        try
        {
            html = await client.GetStringAsync(url);
        }
        catch
        {
            return results;
        }

        // Tüm video bilgilerini HTML'den çıkar
        var videoInfos = ExtractAllVideoInfoFromHtml(html);

        // JSON verilerini çıkar
        var jsonMatch = Regex.Match(html, @"window\.results\s*=\s*(\{.*?\});", RegexOptions.Singleline);
        Dictionary<string, FilmotVideoResult>? resultDict = null;

        if (jsonMatch.Success)
        {
            var jsonStr = jsonMatch.Groups[1].Value;
            try
            {
                resultDict = JsonSerializer.Deserialize<Dictionary<string, FilmotVideoResult>>(jsonStr, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                // JSON parse hatası durumunda devam et
            }
        }

        if (resultDict != null)
        {
            foreach (var video in resultDict.Values)
            {
                // Bu video için HTML'den çıkarılan bilgileri bul
                FilmotVideoInfo? videoInfo = null;
                if (videoInfos.ContainsKey(video.Vid ?? string.Empty))
                {
                    videoInfo = videoInfos[video.Vid ?? string.Empty];
                }

                foreach (var hit in video.Hits ?? Enumerable.Empty<FilmotHit>())
                {
                    var fullText = $"{hit.CtxBefore?.Trim()} {hit.Token?.Trim()} {hit.CtxAfter?.Trim()}".Trim();
                    var urlWithTimestamp = $"https://www.youtube.com/watch?v={video.Vid}&t={(int)hit.Start}s";
                    var baseUrl = $"https://www.youtube.com/watch?v={video.Vid}";
                    var normalizedUrl = HelperService.NormalizeUrl(baseUrl);
                    var contentHash = HelperService.ComputeMd5((hit.Token ?? "") + normalizedUrl);

                    // Title için video başlığını kullan, bulunamazsa token'ı kullan
                    var title = videoInfo?.Title ?? hit.Token ?? string.Empty;

                    // Kanal bilgisi
                    var channelName = videoInfo?.ChannelName ?? "Unknown Channel";
                    var channelId = videoInfo?.ChannelId ?? string.Empty;

                    // Tarih bilgisi
                    var publishDate = videoInfo?.PublishDate ?? DateTime.UtcNow;

                    // Görüntülenme ve beğeni sayıları
                    var viewCount = videoInfo?.ViewCount ?? 0;
                    var likeCount = videoInfo?.LikeCount ?? 0;

                    results.Add(new NewsContent
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
                        ViewCount = (int)viewCount,
                        LikeCount = (int)likeCount
                    });
                }
            }
        }

        return results;
    }

    private Dictionary<string, FilmotVideoInfo> ExtractAllVideoInfoFromHtml(string html)
    {
        var videoInfos = new Dictionary<string, FilmotVideoInfo>();

        // Video kartlarını bulma pattern'i geliştirildi
        var videoCardPattern = @"<div id=""vcard\d+""[^>]*>.*?" +
                              @"<a href=""https://www\.youtube\.com/watch\?v=([^""]+)&t=\d+s""[^>]*>.*?" +
                              @"<div class=""d-inline""[^>]*data-toggle=""tooltip"" title=""([^""]*)"".*?" +
                              @"<button[^>]*onclick=""searchChannel\('([^']*)'\)""[^>]*>.*?" +
                              @"<a href=""/channel/([^""]*)"">([^<]*)</a>.*?" +
                              @"<span class=""badge""><i class=""fa fa-eye""[^>]*></i>([^<]*)</span>.*?" +
                              @"<span class=""badge""><i class=""fa fa-thumbs-up""[^>]*></i>([^<]*)</span>.*?" +
                              @"<span class=""badge"">([^<]*)</span>";

        var videoCardMatches = Regex.Matches(html, videoCardPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

        foreach (Match match in videoCardMatches)
        {
            if (match.Groups.Count >= 9)
            {
                var videoInfo = new FilmotVideoInfo
                {
                    VideoId = match.Groups[1].Value,
                    Title = System.Net.WebUtility.HtmlDecode(match.Groups[2].Value),
                    ChannelId = match.Groups[3].Value,
                    ChannelName = System.Net.WebUtility.HtmlDecode(match.Groups[5].Value),
                    ViewCount = ParseCount(match.Groups[6].Value),
                    LikeCount = ParseCount(match.Groups[7].Value)
                };

                // Tarihi parse et
                var dateString = match.Groups[8].Value.Trim();
                try
                {
                    videoInfo.PublishDate = ParseFilmotDate(dateString);
                }
                catch
                {
                    videoInfo.PublishDate = DateTime.UtcNow;
                }

                videoInfos[videoInfo.VideoId] = videoInfo;
            }
        }

        return videoInfos;
    }

    private DateTime ParseFilmotDate(string dateString)
    {
        // Filmot tarih formatı: "30 Sep 2025"
        var months = new Dictionary<string, int>
        {
            {"Jan", 1}, {"Feb", 2}, {"Mar", 3}, {"Apr", 4}, {"May", 5}, {"Jun", 6},
            {"Jul", 7}, {"Aug", 8}, {"Sep", 9}, {"Oct", 10}, {"Nov", 11}, {"Dec", 12}
        };

        var parts = dateString.Split(' ');
        if (parts.Length == 3)
        {
            if (int.TryParse(parts[0], out int day) &&
                months.ContainsKey(parts[1]) &&
                int.TryParse(parts[2], out int year))
            {
                return new DateTime(year, months[parts[1]], day, 0, 0, 0, DateTimeKind.Utc);
            }
        }

        throw new FormatException($"Invalid date format: {dateString}");
    }

    private long ParseCount(string countString)
    {
        if (string.IsNullOrEmpty(countString))
            return 0;

        // "2.5K", "1.6K", "34.2K" gibi formatları parse et
        countString = countString.Trim().ToUpper();

        if (countString.Contains("K"))
        {
            var numberPart = countString.Replace("K", "").Trim();
            if (double.TryParse(numberPart, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double value))
            {
                return (long)(value * 1000);
            }
        }

        // Sayısal değer
        if (long.TryParse(countString, out long result))
        {
            return result;
        }

        return 0;
    }
}