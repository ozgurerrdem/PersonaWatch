using Microsoft.AspNetCore.Mvc;
using PersonaWatch.Application.Abstraction.Services;
using PersonaWatch.Application.DTOs.UserProfile;

namespace PersonaWatch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserProfilesController : ControllerBase
{
    private readonly IEnumerable<IUserProfiles> _userProfiles;

    public UserProfilesController(IEnumerable<IUserProfiles> userProfiles)
    {
        _userProfiles = userProfiles;
    }

    // GET: api/userprofiles
    [HttpGet]
    public async Task<ActionResult<List<UserProfileDto>>> GetAllProfiles()
    {
        var profiles = new List<UserProfileDto>();

        foreach (var service in _userProfiles)
        {
            var result = await service.GetAllProfilesAsync();
            if (result is not null)
                profiles.AddRange(result);
        }

        return Ok(profiles);
    }

    // POST: api/userprofiles
    // Seçilen kayıt varsa güncelle, yoksa yeni oluştur
    [HttpPost]
    public async Task<IActionResult> UpsertProfile([FromBody] UserProfileDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.PersonName))
            return BadRequest("PersonName is required.");

        foreach (var service in _userProfiles)
        {
            await service.UpsertProfileAsync(dto);
        }

        return Ok();
    }
}