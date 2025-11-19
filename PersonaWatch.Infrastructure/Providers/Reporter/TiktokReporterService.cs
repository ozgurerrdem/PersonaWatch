using System.Text.Json;
using PersonaWatch.Application.Abstraction;
using PersonaWatch.Application.DTOs.Reports;
using PersonaWatch.Domain.Entities;
using PersonaWatch.Infrastructure.Providers.Apify;

namespace PersonaWatch.Infrastructure.Providers.Reporter;

public class TiktokReporterService : IReporter
{
    public string Platform => Platforms.Tiktok.ToString();

    private readonly ApifyClient _apify;

    private const string ACTOR_ID = "0FXVyOXXEmdGcV88a";

    public TiktokReporterService(ApifyClient apify)
    {
        _apify = apify;
    }

    public async Task<ReportBundle> FetchAsync(
        UserProfile profile,
        ReportRequest request,
        CancellationToken ct = default)
    {
        var username = profile.Username?.Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            return new ReportBundle { Platform = Platform };
        }

        var input = new
        {
            excludePinnedPosts = true,
            oldestPostDateUnified = request.FromUtc.ToString("yyyy-MM-dd"),
            profileScrapeSections = new[] { "videos" },
            profileSorting = "latest",
            profiles = new[] { username },
            resultsPerPage = Math.Max(1, request.MaxItemsPerPlatform),
            shouldDownloadAvatars = false,
            shouldDownloadCovers = false,
            shouldDownloadSlideshowImages = false,
            shouldDownloadSubtitles = false,
            shouldDownloadVideos = false
        };

        var runId = await _apify.StartActorRawAsync(ACTOR_ID, input);

        string? datasetId = null;
        var tries = 0;

        while (datasetId == null && tries < 15)
        {
            await Task.Delay(2000, ct);
            datasetId = await _apify.GetDatasetIdAsync(runId);
            tries++;
        }

        if (datasetId == null)
        {
            return new ReportBundle { Platform = Platform };
        }

        var bundle = new ReportBundle { Platform = Platform };

        var items = await _apify.GetDatasetItemsAsync<JsonElement>(datasetId);

        foreach (var item in items)
        {
            try
            {
                var o = item;

                // URL
                var url = o.GetProperty("webVideoUrl").GetString() ?? "";
                if (string.IsNullOrWhiteSpace(url))
                    continue;

                // Tarih
                var createdRaw = o.GetProperty("createTimeISO").GetString();
                if (!DateTime.TryParse(createdRaw, null,
                        System.Globalization.DateTimeStyles.AdjustToUniversal, out var ts))
                {
                    ts = DateTime.UtcNow;
                }
                if (ts.Kind == DateTimeKind.Unspecified)
                    ts = DateTime.SpecifyKind(ts, DateTimeKind.Utc);

                // Tarih filtresi (FromUtc - ToUtc arası)
                if (ts < request.FromUtc || ts > request.ToUtc)
                    continue;

                // Metin
                var text = o.TryGetProperty("text", out var textEl)
                    ? (textEl.GetString() ?? "")
                    : "";

                var report = new ReportItem
                {
                    PostId = ExtractPostId(url),
                    PostUrl = url,
                    Text = text,
                    PublishDateUtc = ts,
                    LikeCount = o.TryGetProperty("diggCount", out var likes) ? likes.GetInt32() : 0,
                    CommentCount = o.TryGetProperty("commentCount", out var c) ? c.GetInt32() : 0,
                    ShareCount = o.TryGetProperty("shareCount", out var s) ? s.GetInt32() : 0,
                    Source = $"apify:{ACTOR_ID}"
                };

                bundle.Items.Add(report);

                if (bundle.Items.Count >= request.MaxItemsPerPlatform)
                    break;
            }
            catch { }
        }

        return bundle;
    }

    private static string ExtractPostId(string url)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(url)) return string.Empty;

            var uri = new Uri(url);
            var parts = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 3 &&
                string.Equals(parts[1], "video", StringComparison.OrdinalIgnoreCase))
            {
                return parts[2];
            }

            return parts.LastOrDefault() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}