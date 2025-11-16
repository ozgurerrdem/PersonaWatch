namespace PersonaWatch.Application.Abstraction;

public interface IUserContext
{
    string UserName { get; }
    bool IsAdmin { get; }
}