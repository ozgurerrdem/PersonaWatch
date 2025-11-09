namespace PersonaWatch.Domain.Entities;

public class NewsContent : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;

    public string Platform { get; set; } = string.Empty;
    public DateTime PublishDate { get; set; }
    public string SearchKeyword { get; set; } = string.Empty;

    public string ContentHash { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
    public int RtCount { get; set; }
    public int QuoteCount { get; set; }
    public int BookmarkCount { get; set; }
    public int DislikeCount { get; set; }
    public int CommentCount { get; set; }
}
