using Microsoft.AspNetCore.Mvc;
using PersonaWatch.Application.Abstraction.Services;
using PersonaWatch.Application.DTOs.Reports;
using PersonaWatch.Domain.Entities;

namespace PersonaWatch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportController : ControllerBase
{
    private readonly IEnumerable<IReports> _reports;

    // Zaten varsa bu ctor’u tekrar eklemene gerek yok
    public ReportController(IEnumerable<IReports> reports)
    {
        _reports = reports;
    }

    // ─────────────────────────────────────────────────────────────
    // INLINE TEST: DB’ye kaydetmeden, elle verilen profil ile IG fetch
    // ─────────────────────────────────────────────────────────────
    public record InstagramInlineRequest(
        string InstagramUsername,
        DateTime FromUtc,
        DateTime ToUtc,
        int? MaxItemsPerPlatform
    );

    [HttpPost("test-instagram-inline")]
    public async Task<IActionResult> TestInstagramInline([FromBody] InstagramInlineRequest dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.InstagramUsername))
            return BadRequest("InstagramUsername boş olamaz.");

        var ig = _reports.FirstOrDefault(x => x.Platform == "instagram");
        if (ig == null)
            return BadRequest("InstagramReportService bulunamadı. DI kaydını kontrol et.");

        // DB’ye dokunmadan geçici profil nesnesi
        var tempProfile = new UserProfile
        {
            DisplayName = dto.InstagramUsername,
            InstagramUsername = dto.InstagramUsername
        };

        var req = new ReportRequest
        {
            FromUtc = dto.FromUtc,
            ToUtc = dto.ToUtc,
            MaxItemsPerPlatform = dto.MaxItemsPerPlatform ?? 30
        };

        var result = await ig.FetchAsync(tempProfile, req, ct);

        return Ok(new
        {
            count = result.Items.Count,
            items = result.Items
        });
    }
}
