using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonaWatch.Infrastructure.Persistence;

namespace PersonaWatch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NewsController : ControllerBase
{
    private readonly AppDbContext _context;

    public NewsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetNews(
        [FromQuery] string? search,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo)
    {
        var query = _context.NewsContents
            .AsNoTracking()
            .Where(n => n.RecordStatus == 'A');

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Aranan anahtar kelime tam eşleşme (mevcut davranışı korudum)
            query = query.Where(n => n.SearchKeyword == search);
            // İçerikte aramak istersen:
            // query = query.Where(n => n.SearchKeyword == search || n.Title.Contains(search) || n.Summary.Contains(search));
        }

        if (dateFrom.HasValue)
        {
            // UI UTC başlangıç gönderiyor ⇒ .Date kullanmadan doğrudan karşılaştır
            query = query.Where(n => n.PublishDate >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(n => n.PublishDate <= dateTo.Value);
        }

        var newsList = await query
            .OrderByDescending(n => n.PublishDate)
            .Select(n => new
            {
                title = n.Title,
                content = n.Summary,
                link = n.Url,
                platform = n.Platform,
                source = n.Source ?? string.Empty,
                publisher = n.Publisher ?? string.Empty,
                publishDate = n.PublishDate,

                likeCount = n.LikeCount,
                rtCount = n.RtCount,
                quoteCount = n.QuoteCount,
                bookmarkCount = n.BookmarkCount,
                dislikeCount = n.DislikeCount,
                viewCount = n.ViewCount,
                commentCount = n.CommentCount
            })
            .ToListAsync();

        return Ok(newsList);
    }


    [HttpGet("search-keywords")]
    public async Task<IActionResult> GetSearchKeywords()
    {
        try
        {
            var keywords = await _context.NewsContents
                                        .AsNoTracking()
                                        .Where(n => n.RecordStatus == 'A')
                                        .Select(n => n.SearchKeyword)
                                        .Distinct()
                                        .OrderBy(k => k)
                                        .ToListAsync();

            if (keywords == null || !keywords.Any())
                return NotFound("No search keywords available.");

            return Ok(keywords);
        }
        catch (Exception)
        {
            return StatusCode(500, "An error occurred while retrieving the search keywords.");
        }
    }
}
