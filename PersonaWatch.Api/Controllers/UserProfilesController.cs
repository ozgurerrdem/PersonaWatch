using Microsoft.AspNetCore.Mvc;
using PersonaWatch.Application.Abstraction;
using PersonaWatch.Application.DTOs.UserProfile;

namespace PersonaWatch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserProfilesController : ControllerBase
{
    private readonly IUserProfiles _userProfileService;

    public UserProfilesController(IUserProfiles userProfileService)
    {
        _userProfileService = userProfileService;
    }

    // GET: api/userprofiles
    [HttpGet]
    public async Task<ActionResult<List<UserProfileDto>>> GetAllProfiles()
    {
        var profiles = await _userProfileService.GetAllProfilesAsync();
        return Ok(profiles);
    }

    // POST: api/userprofiles
    // Seçilen kayıt varsa güncelle, yoksa yeni oluştur
    [HttpPost]
    public async Task<IActionResult> UpsertProfile([FromBody] UserProfileDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.PersonName))
            return BadRequest("PersonName is required.");

        await _userProfileService.UpsertProfileAsync(dto);

        return Ok();
    }
}