using PersonaWatch.Application.DTOs.Reports;
using PersonaWatch.Domain.Entities;

namespace PersonaWatch.Application.Abstraction.Services;

public interface IReports
{
    /// <summary>
    /// Tek bir platformu tarar ve bu platforma ait rapor öğelerini döner.
    /// Uygulamada, birden fazla IReports implementasyonu paralel/ardışık çağrılır.
    /// </summary>
    string Platform { get; } // "facebook" | "instagram" | "x" | "tiktok"

    Task<ReportBundle> FetchAsync(UserProfile profile, ReportRequest request, CancellationToken ct = default);
}
