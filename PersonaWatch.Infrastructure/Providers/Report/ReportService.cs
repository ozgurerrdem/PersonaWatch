using System.Security.Cryptography;
using System.Text;
using PersonaWatch.Application.Abstraction;
using PersonaWatch.Application.DTOs.Reports;
using PersonaWatch.Infrastructure.Persistence;

namespace PersonaWatch.Infrastructure.Providers.Report;

public class ReportService : IReport
{
    private readonly AppDbContext _db;
    private readonly IEnumerable<IReporter> _reporters;

    public ReportService(AppDbContext db, IEnumerable<IReporter> reporters)
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
