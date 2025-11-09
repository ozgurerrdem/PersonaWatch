namespace PersonaWatch.Application.DTOs.Providers.Apify;

public class XTweetsDto
{
    public string? Url { get; set; }
    public string? TwitterUrl { get; set; }
    public string? Id { get; set; }
    public string? Text { get; set; }
    public string? CreatedAt { get; set; }
    public int? RetweetCount { get; set; }
    public int? ReplyCount { get; set; }
    public int? LikeCount { get; set; }
    public int? QuoteCount { get; set; }
    public int? BookmarkCount { get; set; }
    public bool? IsRetweet { get; set; }
    public bool? IsQuote { get; set; }
}
