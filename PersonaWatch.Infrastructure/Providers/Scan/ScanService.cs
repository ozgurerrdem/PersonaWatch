using System.Reflection;
using Microsoft.EntityFrameworkCore;
using PersonaWatch.Application.Abstraction;
using PersonaWatch.Application.DTOs.Scanning;
using PersonaWatch.Infrastructure.Persistence;

namespace PersonaWatch.Infrastructure.Providers.Scan;

public class ScanService : IScan
{
    private readonly AppDbContext _context;
    private readonly IEnumerable<IScanner> _scanners;
    private readonly IUserContext _userContext;

    public ScanService(AppDbContext context, IEnumerable<IScanner> scanners, IUserContext userContext)
    {
        _context = context;
        _scanners = scanners;
        _userContext = userContext;
    }

    public async Task<ScannerResponseDto> ScanAsync(ScannerRequestDto request)
    {
        var allNewContents = new List<Domain.Entities.NewsContent>();
        var exceptions = new List<ScannerExceptions>();

        var existingHashes = new HashSet<string>(
            await _context.NewsContents
                .Where(n => n.RecordStatus == 'A')
                .Select(n => n.ContentHash)
                .ToListAsync(),
            StringComparer.OrdinalIgnoreCase);

        foreach (var scanner in _scanners.Where(scanner => request.ScannerRunCriteria?.Contains(scanner.GetType().Name) == true))
        {
            try
            {
                var contents = await scanner.ScanAsync(request.SearchKeyword ?? string.Empty);

                foreach (var item in contents)
                {
                    if (!existingHashes.Contains(item.ContentHash))
                    {
                        existingHashes.Add(item.ContentHash);
                        item.UpdatedUserName = _userContext.UserName ?? "system";
                        item.CreatedUserName = _userContext.UserName ?? "system";
                        allNewContents.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(new ScannerExceptions
                {
                    ScannerName = scanner.GetType().Name,
                    ErrorMessage = ex.Message
                });
            }
        }

        if (allNewContents.Any())
        {
            _context.NewsContents.AddRange(allNewContents);
            await _context.SaveChangesAsync();
        }

        return new ScannerResponseDto
        {
            NewContents = allNewContents.OrderByDescending(x => x.PublishDate).ToList(),
            Errors = exceptions.Any() ? exceptions : null
        };
    }

    public List<string> GetScanners()
    {
        var interfaceType = typeof(IScanner);
        return Assembly.GetExecutingAssembly()
                        .GetTypes()
                        .Where(t => interfaceType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
                        .Select(t => t.Name)
                        .ToList();
    }
}
