using PersonaWatch.Application.DTOs.UserProfile;

namespace PersonaWatch.Application.Abstraction.Services;

public interface IUserProfiles
{
    Task<List<UserProfileDto>> GetAllProfilesAsync();
    Task UpsertProfileAsync(UserProfileDto dto);
}
