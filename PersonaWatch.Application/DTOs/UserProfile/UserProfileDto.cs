namespace PersonaWatch.Application.DTOs.UserProfile;

public class UserProfileDto
{
    public string PersonName { get; set; } = string.Empty;
    public List<UserProfile>? Profiles { get; set; }
}

public class UserProfile
{
    public string? Username { get; set; }
    public string? Platform { get; set; }
}