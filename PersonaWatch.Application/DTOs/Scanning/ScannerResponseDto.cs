using PersonaWatch.Domain.Entities;

namespace PersonaWatch.Application.DTOs.Scanning;

public class ScannerResponseDto
{
    public List<NewsContent>? NewContents { get; set; }
    public List<ScannerExceptions>? Errors { get; set; }
}

public class ScannerExceptions
{
    public string ScannerName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}