using System.Globalization;
using PersonaWatch.Application.Abstraction;
using PersonaWatch.Application.Common.Helpers;
using PersonaWatch.Application.DTOs.Providers.Apify;
using PersonaWatch.Infrastructure.Providers.Apify;

namespace PersonaWatch.Infrastructure.Providers.Scanners.Apify;

public class XApifyScannerService : IScanner
{
    private readonly ApifyClient _apifyClient;

    public string Source => "XApify";

    public XApifyScannerService(ApifyClient apifyClient)
    {
        _apifyClient = apifyClient;
    }

    public async Task<List<Domain.Entities.NewsContent>> ScanAsync(string searchKeyword)
    {
        var input = new
        {
            maxItems = 50,
            searchTerms = new[] { $"\"{searchKeyword}\"" },
            sort = "Latest"
        };

        var actorId = "nfp1fpt5gUlBwPcor";
        var runId = await _apifyClient.StartActorAsync(actorId, input);

        string? status = null;
        int attempt = 0;
        while (status != "SUCCEEDED" && attempt < 30)
        {
            await Task.Delay(3000);
            status = await _apifyClient.GetRunStatusAsync(runId);
            attempt++;
        }

        if (status != "SUCCEEDED")
            return new List<Domain.Entities.NewsContent>();

        var datasetId = await _apifyClient.GetDatasetIdAsync(runId);
        if (string.IsNullOrWhiteSpace(datasetId))
            return new List<Domain.Entities.NewsContent>();
        var rawTweets = await _apifyClient.GetDatasetItemsAsync<XTweetsDto>(datasetId);

        var results = rawTweets
            .Where(t => !string.IsNullOrWhiteSpace(t.Text) && !string.IsNullOrWhiteSpace(t.Url))
            .Select(t =>
            {
                var url = t.Url ?? string.Empty;
                var title = t.Text!.Length > 100 ? t.Text.Substring(0, 100) : t.Text;

                return new Domain.Entities.NewsContent
                {
                    Id = Guid.NewGuid(),

                    Title = title,
                    Summary = t.Text ?? string.Empty,
                    Url = url,

                    Platform = "X",
                    PublishDate = ParseApifyDate(t.CreatedAt),
                    SearchKeyword = searchKeyword,

                    ContentHash = HelperService.ComputeMd5((t.Text ?? string.Empty) + HelperService.NormalizeUrl(url)),
                    Source = Source,
                    Publisher = ExtractXHandleFromUrl(url),

                    // ---- SAYIM ALANLARI ----
                    LikeCount = t.LikeCount ?? 0,
                    RtCount = t.RetweetCount ?? 0,
                    QuoteCount = t.QuoteCount ?? 0,
                    BookmarkCount = t.BookmarkCount ?? 0,
                    // X API dislike & view vermiyor → 0
                    DislikeCount = 0,
                    ViewCount = 0,
                    // Comment = sadece reply
                    CommentCount = t.ReplyCount ?? 0,

                    // BaseEntity
                    CreatedDate = DateTime.UtcNow,
                    CreatedUserName = "system",
                    RecordStatus = 'A'
                };
            })
            .ToList();

        return results;
    }

    private static string ExtractXHandleFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;
        try
        {
            var u = new Uri(url);
            // /{handle}/status/{id}
            var segments = u.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 1)
            {
                var handle = segments[0];
                if (!string.IsNullOrWhiteSpace(handle) && !string.Equals(handle, "status", StringComparison.OrdinalIgnoreCase))
                    return handle;
            }
        }
        catch { /* ignore */ }
        return string.Empty;
    }

    private static DateTime ParseApifyDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return DateTime.UtcNow;

        if (DateTime.TryParseExact(
            raw,
            "ddd MMM dd HH:mm:ss K yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal,
            out var parsed))
        {
            return parsed;
        }

        return DateTime.UtcNow;
    }
}
