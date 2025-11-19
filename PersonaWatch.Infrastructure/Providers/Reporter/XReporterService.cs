using System.Globalization;
using PersonaWatch.Application.Abstraction;
using PersonaWatch.Application.Common.Helpers;
using PersonaWatch.Application.DTOs.Providers.Apify;
using PersonaWatch.Application.DTOs.Reports;
using PersonaWatch.Domain.Entities;
using PersonaWatch.Infrastructure.Providers.Apify;

namespace PersonaWatch.Infrastructure.Providers.Reporter;

public class XReporterService : IReporter
{
    public string Platform => "X";

    private readonly ApifyClient _apify;

    private const string ACTOR_ID = "nfp1fpt5gUlBwPcor";

    public XReporterService(ApifyClient apify)
    {
        _apify = apify;
    }

    public async Task<ReportBundle> FetchAsync(
        UserProfile profile,
        ReportRequest request,
        CancellationToken ct = default)
    {
        var handle = profile.Username?.Trim();
        if (string.IsNullOrWhiteSpace(handle))
        {
            return new ReportBundle { Platform = Platform };
        }

        var input = new
        {
            maxItems = Math.Max(1, request.MaxItemsPerPlatform),
            searchTerms = new[] { $"from:{handle}" },
            sort = "Latest"
        };

        var runId = await _apify.StartActorAsync(ACTOR_ID, input);

        string? status = null;
        var attempt = 0;

        while (status != "SUCCEEDED" && attempt < 30)
        {
            await Task.Delay(3000, ct);
            status = await _apify.GetRunStatusAsync(runId);
            attempt++;
        }

        if (status != "SUCCEEDED")
            return new ReportBundle { Platform = Platform };

        var datasetId = await _apify.GetDatasetIdAsync(runId);
        if (string.IsNullOrWhiteSpace(datasetId))
            return new ReportBundle { Platform = Platform };

        var rawTweets = await _apify.GetDatasetItemsAsync<XTweetsDto>(datasetId);

        var bundle = new ReportBundle { Platform = Platform };

        foreach (var t in rawTweets)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(t.Text) || string.IsNullOrWhiteSpace(t.Url))
                    continue;

                var url = t.Url!;
                var ts = ParseApifyDate(t.CreatedAt);

                if (ts < request.FromUtc || ts > request.ToUtc)
                    continue;

                var title = t.Text!.Length > 100 ? t.Text.Substring(0, 100) : t.Text;

                var report = new ReportItem
                {
                    PostId = ExtractTweetIdFromUrl(url),
                    PostUrl = url,
                    Text = t.Text ?? string.Empty,
                    PublishDateUtc = ts,
                    LikeCount = t.LikeCount ?? 0,
                    CommentCount = t.ReplyCount ?? 0,
                    ShareCount = t.RetweetCount ?? 0,
                    Source = $"apify:{ACTOR_ID}"
                };

                bundle.Items.Add(report);
            }
            catch { }
        }

        return bundle;
    }

    private static string ExtractTweetIdFromUrl(string url)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(url)) return string.Empty;
            var u = new Uri(url);
            // /{handle}/status/{id} pattern
            var segments = u.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            var idx = Array.FindIndex(segments, s =>
                string.Equals(s, "status", StringComparison.OrdinalIgnoreCase));

            if (idx >= 0 && segments.Length > idx + 1)
                return segments[idx + 1];

            return segments.LastOrDefault() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
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