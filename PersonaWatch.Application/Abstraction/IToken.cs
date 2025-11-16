using PersonaWatch.Domain.Entities;

namespace PersonaWatch.Application.Abstraction;

public interface IToken
{
    string CreateToken(User user);
}