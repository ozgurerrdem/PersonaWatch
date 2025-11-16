using System.Security.Claims;
using PersonaWatch.Application.Abstraction;
namespace PersonaWatch.Api.Security;

public class HttpUserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserName
    {
        get
        {
            var http = _httpContextAccessor.HttpContext;

            // TokenService'de ClaimTypes.Name = user.Username verdiğin için
            var nameClaim = http?.User.FindFirst(ClaimTypes.Name)?.Value
                            ?? http?.User.Identity?.Name;

            // Eski x-username header fallback’i de kalsın istersen:
            var headerUserName = http?.Request.Headers["x-username"].FirstOrDefault();

            return nameClaim
                   ?? headerUserName
                   ?? "system";
        }
    }

    public bool IsAdmin =>
        _httpContextAccessor.HttpContext?.User.FindFirst("isAdmin")?.Value == "true";
}