using System.Text.Json;
using PersonaWatch.Application.Abstraction;
using PersonaWatch.Application.DTOs.Reports;
using PersonaWatch.Domain.Entities;
using PersonaWatch.Infrastructure.Providers.Apify;

namespace PersonaWatch.Infrastructure.Providers.Reporter;

public class FacebookReporterService : IReporter
{
    public string Platform => Platforms.Facebook.ToString();

    private readonly ApifyClient _apify;

    private const string ACTOR_ID = "KoJrdxJCTtpon81KY";

    public FacebookReporterService(ApifyClient apify)
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

        var startUrl = $"https://www.facebook.com/{username.TrimEnd('/')}/";

        var input = new
        {
            captionText = false,
            onlyPostsNewerThan = request.FromUtc.ToString("yyyy-MM-dd"),
            resultsLimit = Math.Max(1, request.MaxItemsPerPlatform),
            startUrls = new[]
            {
                new { url = startUrl }
            }
        };

        // InstagramReporterService ile aynı pattern: StartActorRawAsync + dataset polling
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

                var url = o.GetProperty("url").GetString() ?? "";
                if (string.IsNullOrWhiteSpace(url))
                    continue;

                var text = o.TryGetProperty("text", out var textEl)
                    ? (textEl.GetString() ?? "")
                    : "";

                var likeCount = o.TryGetProperty("likes", out var likesEl)
                    ? likesEl.GetInt32()
                    : 0;

                var commentCount = o.TryGetProperty("comments", out var commentsEl)
                    ? commentsEl.GetInt32()
                    : 0;

                var shareCount = o.TryGetProperty("shares", out var sharesEl)
                    ? sharesEl.GetInt32()
                    : 0;

                var publishDate = ExtractPublishDate(o, request.ToUtc);

                if (publishDate < request.FromUtc || publishDate > request.ToUtc)
                    continue;

                var report = new ReportItem
                {
                    PostId = ExtractPostId(url),
                    PostUrl = url,
                    Text = text,
                    PublishDateUtc = publishDate,
                    LikeCount = likeCount,
                    CommentCount = commentCount,
                    ShareCount = shareCount,
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

            var postsIndex = Array.FindIndex(parts,
                p => string.Equals(p, "posts", StringComparison.OrdinalIgnoreCase));

            if (postsIndex >= 0 && postsIndex < parts.Length - 1)
            {
                return parts[postsIndex + 1];
            }

            return parts.LastOrDefault() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static DateTime ExtractPublishDate(JsonElement o, DateTime fallback)
    {
        try
        {
            if (o.TryGetProperty("timestamp", out var tsEl))
            {
                if (tsEl.ValueKind == JsonValueKind.Number && tsEl.TryGetInt64(out var seconds))
                {
                    return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
                }

                if (tsEl.ValueKind == JsonValueKind.String &&
                    DateTime.TryParse(tsEl.GetString(), out var dt1))
                {
                    return dt1.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(dt1, DateTimeKind.Utc)
                        : dt1.ToUniversalTime();
                }
            }

            if (o.TryGetProperty("createTimeISO", out var isoEl))
            {
                var s = isoEl.GetString();
                if (!string.IsNullOrWhiteSpace(s) &&
                    DateTime.TryParse(s, null,
                        System.Globalization.DateTimeStyles.AdjustToUniversal,
                        out var dt2))
                {
                    return dt2;
                }
            }
        }
        catch { }

        return fallback;
    }
}