namespace PersonaWatch.Application.DTOs.Scanning;

public class ScannerRequestDto
{
    public string? SearchKeyword { get; set; }
    public List<string>? ScannerRunCriteria { get; set; }
}