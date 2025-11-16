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
        throw new NotImplementedException();
    }

    private static string ComputeHash(string input)
    {
        using var sha = SHA1.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hash).Replace("-", "");
    }
}
