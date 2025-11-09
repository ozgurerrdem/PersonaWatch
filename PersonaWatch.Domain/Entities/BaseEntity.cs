namespace PersonaWatch.Domain.Entities;

public abstract class BaseEntity
{
    public string CreatedUserName { get; set; } = "system";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string? UpdatedUserName { get; set; } = "system";
    public DateTime? UpdatedDate { get; set; } = DateTime.UtcNow;
    public char RecordStatus { get; set; } = 'A';
}
