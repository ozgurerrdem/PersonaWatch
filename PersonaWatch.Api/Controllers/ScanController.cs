using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonaWatch.Application.Abstraction;
using PersonaWatch.Application.DTOs.Scanning;

namespace PersonaWatch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScanController : ControllerBase
{
    private readonly IScan _scanService;

    public ScanController(IScan scanService)
    {
        _scanService = scanService;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Scan(ScannerRequestDto request)
    {
        if (string.IsNullOrEmpty(request.SearchKeyword))
            return BadRequest("Missing searchKeyword parameter");
        if (request.ScannerRunCriteria == null || !request.ScannerRunCriteria.Any())
            return BadRequest("Missing scanners parameter");

        var response = await _scanService.ScanAsync(request);
        
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpGet("scanners")]
    public IActionResult GetScanners()
    {
        try
        {
            var scanners = _scanService.GetScanners();
            if (scanners == null || !scanners.Any())
                return NotFound("No scanners available.");

            return Ok(scanners);
        }
        catch (System.Exception)
        {
            return StatusCode(500, "An error occurred while retrieving the scanners.");
        }
    }
}
