namespace PersonaWatch.Application.DTOs.Providers.RapidApi.Filmot;

public class FilmotRapidApiSearchResponse
{
    public int TotalResultCount { get; set; }
    public List<FilmotRapidApiVideoResult> Result { get; set; } = new();
}