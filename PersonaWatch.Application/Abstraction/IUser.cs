using PersonaWatch.Application.DTOs;

namespace PersonaWatch.Application.Abstraction;

public interface IUser
{
    Task<Domain.Entities.User?> GetUserByUsernameAsync(string username);
    Task<List<Domain.Entities.User>> GetAllUsersAsync();
    Task<Domain.Entities.User?> GetUserByIdAsync(Guid id);
    Task UpdateUserAsync(Domain.Entities.User? user, UpdateUserDto dto);
    Task CreateUserAsync(UpdateUserDto dto);
    Task DeleteUserAsync(Domain.Entities.User? user);
}