using System.Text.Json;
using PersonaWatch.Application.Abstraction;
using PersonaWatch.Application.DTOs.Reports;
using PersonaWatch.Domain.Entities;
using PersonaWatch.Infrastructure.Providers.Apify;
    
namespace PersonaWatch.Infrastructure.Providers.Reporter;

public class InstagramReportService : IReporter
{
    public string Platform => "instagram";

    private readonly ApifyClient _apify;

    private const string ACTOR_ID = "nH2AHrwxeTRJoN5hX";

    public InstagramReportService(ApifyClient apify)
    {
        _apify = apify;
    }

    public async Task<ReportBundle> FetchAsync(UserProfile profile, ReportRequest request, CancellationToken ct = default)
    {
        var username = profile.Username?.Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            return new ReportBundle { Platform = Platform };
        }

        // --- Aktör input ---
        var input = new
        {
            onlyPostsNewerThan = request.FromUtc.ToString("yyyy-MM-dd"),
            resultsLimit = Math.Max(1, request.MaxItemsPerPlatform),
            skipPinnedPosts = false,
            username = new[] { username }
        };

        // --- Aktörü çalıştır ve dataset al ---
        var runId = await _apify.StartActorRawAsync(ACTOR_ID, input);
        string? datasetId = null;
        int tries = 0;

        // run tamamlanana kadar poll et (max 30 sn)
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

        var items = await _apify.GetDatasetItemsAsync<JsonElement>(datasetId);

        var bundle = new ReportBundle { Platform = Platform };
        foreach (var item in items)
        {
            try
            {
                var o = item;
                var url = o.GetProperty("url").GetString() ?? "";
                DateTime.TryParse(o.GetProperty("timestamp").GetString(), out var ts);
                if (ts.Kind == DateTimeKind.Unspecified)
                    ts = DateTime.SpecifyKind(ts, DateTimeKind.Utc);

                var report = new ReportItem
                {
                    PostId = ExtractPostId(url),
                    PostUrl = url,
                    Text = o.GetProperty("caption").GetString() ?? "",
                    PublishDateUtc = ts,
                    LikeCount = o.TryGetProperty("likesCount", out var likes) ? likes.GetInt32() : 0,
                    CommentCount = o.TryGetProperty("commentsCount", out var c) ? c.GetInt32() : 0,
                    ShareCount = 0,
                    Source = $"apify:{ACTOR_ID}"
                };

                if (o.TryGetProperty("firstComment", out var fc))
                {
                    var text = fc.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        report.Comments.Add(new CommentItem
                        {
                            CommentId = "first",
                            Author = o.GetProperty("ownerUsername").GetString() ?? "",
                            Text = text!,
                            PublishDateUtc = ts
                        });
                    }
                }

                if (report.PublishDateUtc >= request.FromUtc && report.PublishDateUtc <= request.ToUtc)
                    bundle.Items.Add(report);
            }
            catch (Exception)
            {
            }
        }

        return bundle;
    }

    private static string ExtractPostId(string url)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(url)) return "";
            var uri = new Uri(url);
            var parts = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2 ? parts[1] : parts.FirstOrDefault() ?? "";
        }
        catch { return ""; }
    }
}
