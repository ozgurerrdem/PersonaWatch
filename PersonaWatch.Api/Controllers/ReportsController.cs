using Microsoft.AspNetCore.Mvc;
using PersonaWatch.Application.Abstraction;

namespace PersonaWatch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportController : ControllerBase
{
    private readonly IEnumerable<IReport> _report;

    // Zaten varsa bu ctor’u tekrar eklemene gerek yok
    public ReportController(IEnumerable<IReport> report)
    {
        _report = report;
    }
}
    