using PersonaWatch.Application.Abstraction;
using PersonaWatch.Application.Common.Helpers;
using PersonaWatch.Application.DTOs.Providers.Apify;
using PersonaWatch.Infrastructure.Providers.Apify;
using PersonaWatch.Infrastructure.Providers.Reporter;

namespace PersonaWatch.Infrastructure.Providers.Scanners.Apify;

public class TiktokApifyScannerService : IScanner
{
    private readonly ApifyClient _apifyClient;
    public string Source => Platforms.Tiktok.ToString();

    public TiktokApifyScannerService(ApifyClient apifyClient)
    {
        _apifyClient = apifyClient;
    }

    public async Task<List<Domain.Entities.NewsContent>> ScanAsync(string searchKeyword)
    {
        var results = new List<Domain.Entities.NewsContent>();
        var actorId = "GdWCkxBtKWOsKjdch";

        var input = new
        {
            excludePinnedPosts = false,
            hashtags = new[] { $"\"{searchKeyword}\"" },
            proxyCountryCode = "None",
            resultsPerPage = 100,
            scrapeRelatedVideos = false,
            shouldDownloadAvatars = false,
            shouldDownloadCovers = false,
            shouldDownloadMusicCovers = false,
            shouldDownloadSlideshowImages = false,
            shouldDownloadSubtitles = false,
            shouldDownloadVideos = false,
            profileScrapeSections = new[] { "videos" },
            profileSorting = "latest",
            searchSection = string.Empty,
            maxProfilesPerQuery = 10
        };

        var runId = await _apifyClient.StartActorRawAsync(actorId, input);

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

        var rawItems = await _apifyClient.GetDatasetItemsAsync<TiktokDto>(datasetId);

        results.AddRange(
            rawItems
                .Where(p => !string.IsNullOrWhiteSpace(p.Text) && !string.IsNullOrWhiteSpace(p.WebVideoUrl))
                .Select(p =>
                {
                    var url = HelperService.NormalizeUrl(p.WebVideoUrl ?? string.Empty);
                    var title = (p.Text ?? string.Empty);
                    title = title.Length > 100 ? title[..100] : title;
                    if (string.IsNullOrWhiteSpace(title))
                        title = p.AuthorMeta?.Name ?? p.AuthorNameFlat ?? "TikTok Gönderisi";

                    return new Domain.Entities.NewsContent
                    {
                        Id = Guid.NewGuid(),

                        Title = title,
                        Summary = p.Text ?? string.Empty,
                        Url = url,

                        Platform = "TikTok",
                        PublishDate = ConvertToUtc(p.CreateTimeISO),
                        SearchKeyword = searchKeyword,

                        ContentHash = HelperService.ComputeMd5((p.Text ?? string.Empty) + url),

                        Source = Source,
                        Publisher = p.AuthorMeta?.Name ?? p.AuthorNameFlat ?? string.Empty,

                        // Sayaçlar
                        LikeCount = p.DiggCount ?? 0,
                        RtCount = p.ShareCount ?? 0,
                        ViewCount = p.PlayCount ?? 0,
                        CommentCount = p.CommentCount ?? 0,
                        BookmarkCount = p.CollectCount ?? 0,
                        QuoteCount = 0,
                        DislikeCount = 0,

                        // BaseEntity
                        CreatedDate = DateTime.UtcNow,
                        CreatedUserName = "system",
                        RecordStatus = 'A'
                    };
                })
        );

        return results;
    }

    private static DateTime ConvertToUtc(string? isoDate)
    {
        if (DateTime.TryParse(isoDate, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var dt))
            return dt.ToUniversalTime();

        return DateTime.UtcNow;
    }
}
