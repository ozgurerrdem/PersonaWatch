namespace PersonaWatch.Application.DTOs.Reports;

public class CommentItem
{
    public string CommentId { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime PublishDateUtc { get; set; }
}