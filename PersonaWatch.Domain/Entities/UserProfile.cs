namespace PersonaWatch.Domain.Entities;

public class UserProfile : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DisplayName { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public string? FacebookUserId { get; set; }
    public string? FacebookUsername { get; set; }

    public string? InstagramUserId { get; set; }
    public string? InstagramUsername { get; set; }

    public string? XUserId { get; set; }
    public string? XUsername { get; set; }

    public string? TikTokUserId { get; set; }
    public string? TikTokUsername { get; set; }
    public DateTime? LastFacebookFetchUtc { get; set; }
    public DateTime? LastInstagramFetchUtc { get; set; }
    public DateTime? LastXFetchUtc { get; set; }
    public DateTime? LastTikTokFetchUtc { get; set; }
}
