namespace PersonaWatch.Application.DTOs.Reports;

public class ReportItem
{
    public string PostId { get; set; } = string.Empty;
    public string PostUrl { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime PublishDateUtc { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public int ShareCount { get; set; }
    public List<CommentItem> Comments { get; set; } = new();
    public string Source { get; set; } = string.Empty; // aktör/scanner adı
}
