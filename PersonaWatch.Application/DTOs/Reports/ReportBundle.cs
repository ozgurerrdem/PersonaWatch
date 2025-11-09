namespace PersonaWatch.Application.DTOs.Reports;

public class ReportBundle
{
    public string Platform { get; set; } = string.Empty;
    public List<ReportItem> Items { get; set; } = new();
}