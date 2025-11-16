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
}
