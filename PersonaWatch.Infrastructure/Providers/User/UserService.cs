using Microsoft.AspNetCore.Identity;
using PersonaWatch.Application.Abstraction;
using PersonaWatch.Application.DTOs;
using PersonaWatch.Infrastructure.Persistence;

namespace PersonaWatch.Infrastructure.Providers.User;

public class UserService : IUser
{
    private readonly AppDbContext _context;
    private readonly IUserContext _userContext;
    public UserService(AppDbContext context, IUserContext userContext)
    {
        _context = context;
        _userContext = userContext;
    }

    public async Task<Domain.Entities.User?> GetUserByUsernameAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return null;

        return _context.Users.Where(u => u.RecordStatus == 'A').FirstOrDefault(u => u.Username == username);
    }

    public async Task<List<Domain.Entities.User>> GetAllUsersAsync()
    {
        if (string.IsNullOrWhiteSpace(_userContext.UserName))
            return null;

        return _context.Users
            .Where(u => u.RecordStatus == 'A')
            .Select(u => new Domain.Entities.User
            {
                Id = u.Id,
                Username = u.Username,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsAdmin = u.IsAdmin
            })
            .OrderBy(u => u.Username)
            .ThenBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToList();
    }

    public async Task<Domain.Entities.User?> GetUserByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            return null;

        return await _context.Users.FindAsync(id);
    }

    public async Task UpdateUserAsync(Domain.Entities.User? user, UpdateUserDto dto)
    {
        ArgumentNullException.ThrowIfNull(user);

        user.Username = dto.Username;
        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.IsAdmin = dto.IsAdmin;

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            var hasher = new PasswordHasher<Domain.Entities.User?>();
            user.Password = hasher.HashPassword(user, dto.Password);
        }

        user.UpdatedUserName = _userContext.UserName ?? "system";
        user.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task CreateUserAsync(UpdateUserDto dto)
    {
        var hasher = new PasswordHasher<Domain.Entities.User>();
        var user = new Domain.Entities.User
        {
            Username = dto.Username,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            IsAdmin = dto.IsAdmin,
            Password = hasher.HashPassword(null!, dto.Password),
            CreatedUserName = _userContext.UserName ?? "system",
            CreatedDate = DateTime.UtcNow,
            UpdatedUserName = _userContext.UserName ?? "system",
            UpdatedDate = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteUserAsync(Domain.Entities.User? user)
    {
        ArgumentNullException.ThrowIfNull(user);

        user.RecordStatus = 'P';
        user.UpdatedUserName = _userContext.UserName ?? "system";
        user.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }
}