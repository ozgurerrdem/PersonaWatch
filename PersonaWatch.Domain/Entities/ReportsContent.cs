namespace PersonaWatch.Domain.Entities;

public class ReportsContent : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserProfileId { get; set; }
    public UserProfile? UserProfile { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string PostId { get; set; } = string.Empty;
    public string PostUrl { get; set; } = string.Empty;
    public string PostText { get; set; } = string.Empty;
    public DateTime PostPublishDate { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public int ShareCount { get; set; }
    public string? CommentsJson { get; set; }
    public DateTime ReportFromUtc { get; set; }
    public DateTime ReportToUtc { get; set; }
    public string Source { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
}
