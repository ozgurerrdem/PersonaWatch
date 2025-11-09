namespace PersonaWatch.Application.DTOs.Reports;

public class ReportRequest
{
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
    public int MaxItemsPerPlatform { get; set; } = 200; // güvenli bir default
}
