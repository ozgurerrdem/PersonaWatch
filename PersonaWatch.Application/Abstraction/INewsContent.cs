namespace PersonaWatch.Application.Abstraction;

public interface INewsContent
{
    Task<IEnumerable<Domain.Entities.NewsContent>> GetAllNewsContents();
    Task<List<string>> GetAllSearchKeywords();
}