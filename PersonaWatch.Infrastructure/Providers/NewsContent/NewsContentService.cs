using Microsoft.EntityFrameworkCore;
using PersonaWatch.Application.Abstraction;
using PersonaWatch.Infrastructure.Persistence;

namespace PersonaWatch.Infrastructure.Providers.NewsContent;

public class NewsContentService : INewsContent
{
    private readonly AppDbContext _context;

    public NewsContentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Domain.Entities.NewsContent>> GetAllNewsContents()
    {
        return await _context.NewsContents
            .AsNoTracking()
            .Where(n => n.RecordStatus == 'A')
            .ToListAsync();
    }

    public async Task<List<string>> GetAllSearchKeywords()
    {
        return await _context.NewsContents
                .AsNoTracking()
                .Where(n => n.RecordStatus == 'A')
                .Select(n => n.SearchKeyword)
                .Distinct()
                .OrderBy(k => k)
                .ToListAsync();
    }
}