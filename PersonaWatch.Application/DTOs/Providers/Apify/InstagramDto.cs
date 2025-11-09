namespace PersonaWatch.Application.DTOs.Providers.Apify;

public class InstagramDto
{
    public string? Caption { get; set; }
    public string? OwnerFullName { get; set; }
    public string? OwnerUsername { get; set; }
    public string? Url { get; set; }
    public int? CommentsCount { get; set; }
    public string? FirstComment { get; set; }
    public int? LikesCount { get; set; }
    public string? Timestamp { get; set; }
    public List<string>? Hashtags { get; set; }
}
