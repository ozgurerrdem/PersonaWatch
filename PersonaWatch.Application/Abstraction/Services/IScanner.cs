using PersonaWatch.Domain.Entities;

namespace PersonaWatch.Application.Abstraction.Services;

public interface IScanner
{
    Task<List<NewsContent>> ScanAsync(string searchKeyword);
    string Source { get; }
}
