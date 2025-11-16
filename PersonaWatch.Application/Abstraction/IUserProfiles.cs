using PersonaWatch.Application.DTOs.UserProfile;

namespace PersonaWatch.Application.Abstraction;

public interface IUserProfiles
{
    Task<List<UserProfileDto>> GetAllProfilesAsync();
    Task UpsertProfileAsync(UserProfileDto dto);
}
