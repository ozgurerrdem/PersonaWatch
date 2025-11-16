using PersonaWatch.Application.DTOs.Reports;

namespace PersonaWatch.Application.Abstraction;

public interface IReport
{
    Task<(int saved, int skipped)> RunAsync(Guid userProfileId, ReportRequest req, CancellationToken ct = default);
}