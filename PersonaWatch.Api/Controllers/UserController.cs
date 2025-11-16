using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PersonaWatch.Application.Abstraction;
using PersonaWatch.Application.DTOs;
using PersonaWatch.Domain.Entities;

namespace PersonaWatch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IToken _tokenService;
    private readonly IUser _userService;
    private readonly IUserContext _userContext;

    public UserController(IToken tokenService, IUser userService, IUserContext userContext)
    {
        _tokenService = tokenService;
        _userService = userService;
        _userContext = userContext;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto dto)
    {
        var user = await _userService.GetUserByUsernameAsync(dto.Username);
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
    public async Task<IActionResult> GetAllUsers()
    {
        if (!IsCurrentUserAdmin())
            return Forbid();

        var users = await _userService.GetAllUsersAsync();

        return Ok(users);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
    {
        if (!IsCurrentUserAdmin())
            return Forbid();

        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
            return NotFound();

        if (user.Username == "admin" && _userContext.UserName != "admin")
            return BadRequest("Admin kullanıcısı güncellenemez.");

        await _userService.UpdateUserAsync(user, dto);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] UpdateUserDto dto)
    {
        if (!IsCurrentUserAdmin())
            return Forbid();

        if (string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest("Şifre zorunludur.");

        var exists = await _userService.GetUserByUsernameAsync(dto.Username) != null;
        if (exists)
            return Conflict("Bu kullanıcı adı zaten var.");

        await _userService.CreateUserAsync(dto);

        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUserAsync(Guid id)
    {
        if (!IsCurrentUserAdmin())
            return Forbid();

        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
            return NotFound();

        if (user.Username.ToLower() == "admin")
            return BadRequest("Admin kullanıcısı silinemez.");

        await _userService.DeleteUserAsync(user);        
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
