namespace PersonaWatch.Application.DTOs.Providers.Filmot;

public class FilmotVideoInfo
{
    public string VideoId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    public long ViewCount { get; set; }
    public long LikeCount { get; set; }
    public DateTime PublishDate { get; set; }
}