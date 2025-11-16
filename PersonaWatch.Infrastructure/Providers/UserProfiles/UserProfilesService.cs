using Microsoft.EntityFrameworkCore;
using PersonaWatch.Application.Abstraction;
using PersonaWatch.Application.DTOs.UserProfile;
using PersonaWatch.Infrastructure.Persistence;

namespace PersonaWatch.Infrastructure.Providers.UserProfiles;

public class UserProfilesService : IUserProfiles
{
    private readonly AppDbContext _context;
    private readonly IUserContext _userContext;
    public UserProfilesService(AppDbContext context, IUserContext userContext)
    {
        _context = context;
        _userContext = userContext;
    }
    public async Task<List<UserProfileDto>> GetAllProfilesAsync()
    {
        var profiles = await _context.UserProfiles.ToListAsync();

        return profiles.GroupBy(p => p.PersonName)
            .Select(g => new UserProfileDto
            {
                PersonName = g.Key,
                Profiles = g.Select(p => new UserProfile
                {
                    Username = p.Username,
                    Platform = p.Platform
                }).ToList()
            }).ToList();
    }

    public async Task UpsertProfileAsync(UserProfileDto dto)
    {
        if (dto is null)
            throw new ArgumentNullException(nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.PersonName))
            throw new ArgumentException("PersonName is required.", nameof(dto));

        var existing = await _context.UserProfiles
            .Where(p => p.PersonName == dto.PersonName)
            .ToListAsync();

        var existingByPlatform = existing
            .Where(e => !string.IsNullOrWhiteSpace(e.Platform))
            .ToDictionary(
                e => e.Platform!.Trim(),
                e => e,
                StringComparer.OrdinalIgnoreCase);

        var incoming = dto.Profiles?
                        .Where(p =>
                            !string.IsNullOrWhiteSpace(p.Platform) &&
                            !string.IsNullOrWhiteSpace(p.Username))
                        .ToList()
                        ?? new List<Application.DTOs.UserProfile.UserProfile>();

        foreach (var profile in incoming)
        {
            var platform = profile.Platform?.Trim();
            var username = profile.Username?.Trim();

            if (string.IsNullOrWhiteSpace(platform) || string.IsNullOrWhiteSpace(username))
                continue;

            if (existingByPlatform.TryGetValue(platform, out var entity))
            {
                var existingUsername = entity.Username?.Trim();

                if (!string.Equals(existingUsername, username, StringComparison.OrdinalIgnoreCase))
                {
                    entity.Username = username;
                    entity.UpdatedDate = DateTime.Now;
                    entity.UpdatedUserName = _userContext.UserName ?? "system";
                }
            }
            else
            {
                var entityToSave = new PersonaWatch.Domain.Entities.UserProfile
                {
                    PersonName = dto.PersonName,
                    Platform = platform,
                    Username = username,
                    CreatedDate = DateTime.Now,
                    CreatedUserName = _userContext.UserName ?? "system",
                    UpdatedDate = DateTime.Now,
                    UpdatedUserName = _userContext.UserName ?? "system"
                };

                await _context.UserProfiles.AddAsync(entityToSave);
                existingByPlatform[platform] = entityToSave;
            }
        }

        await _context.SaveChangesAsync();
    }
}