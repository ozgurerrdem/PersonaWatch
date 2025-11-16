using PersonaWatch.Application.DTOs.Scanning;

namespace PersonaWatch.Application.Abstraction;

public interface IScan
{
    Task<ScannerResponseDto> ScanAsync(ScannerRequestDto request);
    List<string> GetScanners();
}