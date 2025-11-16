using Microsoft.AspNetCore.Mvc;
using PersonaWatch.Application.Abstraction;

namespace PersonaWatch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ToolsController : ControllerBase
{
    private readonly IConfiguration _cfg;
    private readonly IClipService _clipService;

    public ToolsController(IConfiguration cfg, IClipService clipService)
    {
        _cfg = cfg;
        _clipService = clipService;
    }

    [HttpGet("youtube/clip")]
    public async Task<IActionResult> Clip([FromQuery] string videoId, [FromQuery] int start, [FromQuery] int end, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(videoId)) return BadRequest("videoId gereklidir.");
        if (start < 0 || end <= 0 || end <= start) return BadRequest("Geçersiz zaman aralığı.");

        var max = _cfg.GetValue<int?>("Tools:MaxClipSeconds") ?? 300;
        if (end - start > max) return BadRequest($"Maksimum süre {max} sn.");

        var result = await _clipService.ClipAsync(videoId, start, end, ct);
        // ASP.NET tarafında uygun dönüş:
        return File(result.Content, result.ContentType, result.FileName, enableRangeProcessing: true);
    }
}
