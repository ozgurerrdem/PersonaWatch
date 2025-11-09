using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PersonaWatch.Application.Abstraction.Services;
using PersonaWatch.Application.Common.Helpers;
using PersonaWatch.Application.DTOs.Reports;
using PersonaWatch.Domain.Entities;
using PersonaWatch.Infrastructure.Persistence;

namespace PersonaWatch.Infrastructure.Providers.Reports;

public class ReportService
{
    private readonly AppDbContext _db;
    private readonly IEnumerable<IReports> _reporters;

    public ReportService(AppDbContext db, IEnumerable<IReports> reporters)
    {
        _db = db;
        _reporters = reporters;
    }

    public async Task<(int saved, int skipped)> RunAsync(Guid userProfileId, ReportRequest req, CancellationToken ct = default)
    {
        var profile = await _db.UserProfiles.FirstOrDefaultAsync(x => x.Id == userProfileId, ct)
                      ?? throw new InvalidOperationException("UserProfile not found");

        int saved = 0, skipped = 0;

        foreach (var reporter in _reporters)
        {
            // Platform’a uygunluk (örn kullanıcıda o platform kullanıcı adı/id’si var mı?)
            if (!IsPlatformEnabled(profile, reporter.Platform)) continue;

            var bundle = await reporter.FetchAsync(profile, req, ct);
            foreach (var item in bundle.Items)
            {
                var entity = new ReportsContent
                {
                    UserProfileId = profile.Id,
                    Platform = bundle.Platform,
                    PostId = item.PostId,
                    PostUrl = item.PostUrl,
                    PostText = item.Text,
                    PostPublishDate = item.PublishDateUtc,
                    LikeCount = item.LikeCount,
                    CommentCount = item.CommentCount,
                    ShareCount = item.ShareCount,
                    CommentsJson = System.Text.Json.JsonSerializer.Serialize(item.Comments),
                    ReportFromUtc = req.FromUtc,
                    ReportToUtc = req.ToUtc,
                    Source = item.Source
                };

                var normalizedUrl = HelperService.NormalizeUrl(item.PostUrl ?? "");
                entity.ContentHash = HelperService.ComputeMd5(normalizedUrl);

                // Tekilleştir
                var exists = await _db.ReportsContents
                    .AnyAsync(x => x.ContentHash == entity.ContentHash, ct);

                if (exists) { skipped++; continue; }

                _db.ReportsContents.Add(entity);
                saved++;
            }
        }

        await _db.SaveChangesAsync(ct);
        return (saved, skipped);
    }

    private static string ComputeHash(string input)
    {
        using var sha = SHA1.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hash).Replace("-", "");
    }

    private static bool IsPlatformEnabled(UserProfile p, string platform)
    {
        platform = platform.ToLowerInvariant();
        return platform switch
        {
            "facebook" => !string.IsNullOrWhiteSpace(p.FacebookUserId) || !string.IsNullOrWhiteSpace(p.FacebookUsername),
            "instagram" => !string.IsNullOrWhiteSpace(p.InstagramUserId) || !string.IsNullOrWhiteSpace(p.InstagramUsername),
            "x" => !string.IsNullOrWhiteSpace(p.XUserId) || !string.IsNullOrWhiteSpace(p.XUsername),
            "tiktok" => !string.IsNullOrWhiteSpace(p.TikTokUserId) || !string.IsNullOrWhiteSpace(p.TikTokUsername),
            _ => false
        };
    }
}
