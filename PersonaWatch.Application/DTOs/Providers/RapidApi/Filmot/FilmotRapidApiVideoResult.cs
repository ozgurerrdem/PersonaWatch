using PersonaWatch.Application.DTOs.Providers.Filmot;

namespace PersonaWatch.Application.DTOs.Providers.RapidApi.Filmot;

public class FilmotRapidApiVideoResult
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public int Duration { get; set; }
    public string? Uploaddate { get; set; }
    public long Viewcount { get; set; }
    public long Likecount { get; set; }
    public string? Category { get; set; }
    public string? Channelname { get; set; }
    public long Channelsubcount { get; set; }
    public string? Channelid { get; set; }
    public string? Lang { get; set; }

    public List<FilmotHit>? Hits { get; set; }
}