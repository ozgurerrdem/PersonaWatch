namespace PersonaWatch.Application.Abstraction;

public interface IClipService
{
    /// <summary>
    /// Verilen video aralığından bir MP4 klip üretir ve sonucu döner.
    /// </summary>
    Task<ClipResult> ClipAsync(
        string videoId,
        int start,
        int end,
        CancellationToken ct = default);
}

/// <summary>Web’den bağımsız, aktarılabilir sonuç.</summary>
public sealed record ClipResult(Stream Content, string FileName, string ContentType);
