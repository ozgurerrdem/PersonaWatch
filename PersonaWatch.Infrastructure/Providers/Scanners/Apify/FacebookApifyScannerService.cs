using System.Globalization;
using PersonaWatch.Application.Abstraction.Services;
using PersonaWatch.Application.Common.Helpers;
using PersonaWatch.Application.DTOs.Providers.Apify;
using PersonaWatch.Domain.Entities;
using PersonaWatch.Infrastructure.Providers.Apify;

namespace PersonaWatch.Infrastructure.Providers.Scanners.Apify;

public class FacebookApifyScannerService : IScanner
{
    private readonly ApifyClient _apifyClient;
    public string Source => "FacebookApify";

    public FacebookApifyScannerService(ApifyClient apifyClient)
    {
        _apifyClient = apifyClient;
    }

    public async Task<List<NewsContent>> ScanAsync(string searchKeyword)
    {
        var results = new List<NewsContent>();
        var actorId = "4YfcIWyRtJHJ5Ha3a";

        var input = new
        {
            searchQuery = searchKeyword,
            maxPosts = 10
        };

        var runId = await _apifyClient.StartActorAsync(actorId, input);

        string? status = null;
        int attempt = 0;
        while (status != "SUCCEEDED" && attempt < 30)
        {
            await Task.Delay(3000);
            status = await _apifyClient.GetRunStatusAsync(runId);
            attempt++;
        }

        if (status != "SUCCEEDED") return results;

        var datasetId = await _apifyClient.GetDatasetIdAsync(runId);
        if (string.IsNullOrWhiteSpace(datasetId)) return results;

        var rawItems = await _apifyClient.GetDatasetItemsAsync<FacebookDto>(datasetId);

        results.AddRange(
            rawItems
                .Where(p => !string.IsNullOrWhiteSpace(p.Text) && (!string.IsNullOrWhiteSpace(p.Url) || !string.IsNullOrWhiteSpace(p.TopLevelUrl) || !string.IsNullOrWhiteSpace(p.FacebookUrl)))
                .Select(p =>
                {
                    var postUrl = p.Url
                                  ?? p.TopLevelUrl
                                  ?? p.FacebookUrl
                                  ?? string.Empty;

                    // Başlık: metnin ilk 100 karakteri
                    var titleSource = p.Text ?? string.Empty;
                    var title = titleSource.Length > 100 ? titleSource[..100] : titleSource;

                    return new NewsContent
                    {
                        Id = Guid.NewGuid(),

                        Title = string.IsNullOrWhiteSpace(title) ? (p.PageName ?? "Facebook Gönderisi") : title,
                        Summary = p.Text ?? string.Empty,
                        Url = postUrl,

                        Platform = "Facebook",
                        PublishDate = ConvertFromUnixOrTime(p.Timestamp, p.Time),
                        SearchKeyword = searchKeyword,

                        ContentHash = HelperService.ComputeMd5(
                            (p.Text ?? string.Empty) + HelperService.NormalizeUrl(postUrl)
                        ),

                        Source = Source,
                        Publisher = p.PageName ?? string.Empty,

                        // ====== SAYIM ALANLARI ======
                        LikeCount = p.Likes ?? 0,
                        CommentCount = p.Comments ?? 0,
                        RtCount = p.Shares ?? 0,
                        QuoteCount = 0,
                        BookmarkCount = 0,
                        DislikeCount = 0,
                        ViewCount = 0,

                        // BaseEntity
                        CreatedDate = DateTime.UtcNow,
                        CreatedUserName = "system",
                        RecordStatus = 'A'
                    };
                })
        );

        return results;
    }

    private static DateTime ConvertFromUnixOrTime(long? timestamp, string? time)
    {
        // Öncelik: Unix timestamp (saniye)
        if (timestamp.HasValue && timestamp > 0)
            return DateTimeOffset.FromUnixTimeSeconds(timestamp.Value).UtcDateTime;

        // Alternatif: Formatlı tarih dizesi (APIfy "time" alanı)
        if (!string.IsNullOrWhiteSpace(time) &&
            DateTime.TryParse(time, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
        {
            return parsed;
        }

        return DateTime.UtcNow;
    }
}
