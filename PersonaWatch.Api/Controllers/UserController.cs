using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PersonaWatch.Application.DTOs;
using PersonaWatch.Domain.Entities;
using PersonaWatch.Infrastructure.Persistence;
using PersonaWatch.Infrastructure.Security;

namespace PersonaWatch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TokenService _tokenService;

    public UserController(AppDbContext context, TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public IActionResult Login([FromBody] UserLoginDto dto)
    {
        var user = _context.Users.Where(u => u.RecordStatus == 'A').FirstOrDefault(u => u.Username == dto.Username);
        if (user == null)
            return Unauthorized("Kullanıcı bulunamadı");

        var hasher = new PasswordHasher<User>();
        var result = hasher.VerifyHashedPassword(user, user.Password, dto.Password);

        if (result == PasswordVerificationResult.Failed)
            return Unauthorized("Şifre hatalı");

        var token = _tokenService.CreateToken(user);

        return Ok(new
        {
            token,
            username = user.Username,
            firstName = user.FirstName,
            lastName = user.LastName,
            isAdmin = user.IsAdmin
        });
    }

    [HttpGet("all")]
    public IActionResult GetAllUsers()
    {
        if (!IsCurrentUserAdmin())
            return Forbid();

        var users = _context.Users
            .Where(u => u.RecordStatus == 'A')
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.FirstName,
                u.LastName,
                u.IsAdmin
            })
            .OrderBy(u => u.Username)
            .ThenBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToList();

        return Ok(users);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
    {
        if (!IsCurrentUserAdmin())
            return Forbid();

        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        if (user.Username.ToLower() == "admin")
            return BadRequest("Admin kullanıcısı güncellenemez.");

        user.Username = dto.Username;
        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.IsAdmin = dto.IsAdmin;

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            var hasher = new PasswordHasher<User>();
            user.Password = hasher.HashPassword(user, dto.Password);
        }

        user.UpdatedUserName = Request.Headers["x-username"].ToString() ?? "system";
        user.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] UpdateUserDto dto)
    {
        if (!IsCurrentUserAdmin())
            return Forbid();

        if (string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest("Şifre zorunludur.");

        var exists = _context.Users.Where(u => u.RecordStatus == 'A').Any(u => u.Username == dto.Username);
        if (exists)
            return Conflict("Bu kullanıcı adı zaten var.");

        var hasher = new PasswordHasher<User>();
        var user = new User
        {
            Username = dto.Username,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            IsAdmin = dto.IsAdmin,
            Password = hasher.HashPassword(null!, dto.Password),
            CreatedUserName = Request.Headers["x-username"].FirstOrDefault() ?? "system",
            CreatedDate = DateTime.UtcNow,
            UpdatedUserName = Request.Headers["x-username"].FirstOrDefault() ?? "system",
            UpdatedDate = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> SoftDeleteUser(Guid id)
    {
        if (!IsCurrentUserAdmin())
            return Forbid();

        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        if (user.Username.ToLower() == "admin")
            return BadRequest("Admin kullanıcısı silinemez.");

        user.RecordStatus = 'P';
        user.UpdatedUserName = Request.Headers["x-username"].FirstOrDefault() ?? "system";
        user.UpdatedDate = DateTime.Now;

        await _context.SaveChangesAsync();
        return Ok();
    }

    [Authorize]
    [HttpGet("validate")]
    public IActionResult ValidateToken()
    {
        return Ok(true);
    }

    private bool IsCurrentUserAdmin()
    {
        return User.Claims.FirstOrDefault(c => c.Type == "isAdmin")?.Value == "true";
    }
}
