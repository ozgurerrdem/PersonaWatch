namespace PersonaWatch.Domain.Entities;

public class UserProfile : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PersonName { get; set; } = string.Empty;
    public string? Platform { get; set; }
    public string? Username { get; set; }
    public DateTime? LastFetchDate { get; set; }
}
