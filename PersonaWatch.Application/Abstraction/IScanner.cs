namespace PersonaWatch.Application.Abstraction;

public interface IScanner
{
    Task<List<Domain.Entities.NewsContent>> ScanAsync(string searchKeyword);
    string Source { get; }
}
