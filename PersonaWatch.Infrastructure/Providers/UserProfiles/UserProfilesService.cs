using Microsoft.EntityFrameworkCore;
using PersonaWatch.Application.Abstraction.Services;
using PersonaWatch.Application.DTOs.UserProfile;
using PersonaWatch.Infrastructure.Persistence;

namespace PersonaWatch.Infrastructure.Providers.UserProfiles;

public class UserProfilesService : IUserProfiles
{
    private readonly AppDbContext _context;
    public UserProfilesService(AppDbContext context)
    {
        _context = context;
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

        // Bu kişiye ait mevcut kayıtlar
        var existing = await _context.UserProfiles
            .Where(p => p.PersonName == dto.PersonName)
            .ToListAsync();

        // Platform bazlı lookup (case-insensitive)
        var existingByPlatform = existing
            .Where(e => !string.IsNullOrWhiteSpace(e.Platform))
            .ToDictionary(
                e => e.Platform!.Trim(),
                e => e,
                StringComparer.OrdinalIgnoreCase);

        var incoming = dto.Profiles ?? new List<Application.DTOs.UserProfile.UserProfile>();

        foreach (var profile in incoming)
        {
            var platform = profile.Platform?.Trim();
            var username = profile.Username?.Trim();

            // Platform yoksa / boşsa tamamen ignore
            if (string.IsNullOrWhiteSpace(platform))
                continue;

            // Username boşsa bu platform için hiçbir şey yapma (sadece dolu olanlar işleniyor)
            if (string.IsNullOrWhiteSpace(username))
                continue;

            if (existingByPlatform.TryGetValue(platform, out var entity))
            {
                // Güncelle
                entity.Username = username;
                // DTO’da LastFetchDate yoksa bunu set etme; varsa ekleyebilirsin.
                // entity.LastFetchDate = profile.LastFetchDate;
            }
            else
            {
                // Yeni kayıt
                var entityToSave = new PersonaWatch.Domain.Entities.UserProfile
                {
                    PersonName = dto.PersonName,
                    Platform = platform,
                    Username = username,
                    // LastFetchDate = profile.LastFetchDate
                };

                await _context.UserProfiles.AddAsync(entityToSave);
                existingByPlatform[platform] = entityToSave;
            }
        }

        await _context.SaveChangesAsync();
    }
}