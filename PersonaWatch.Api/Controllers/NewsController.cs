using Microsoft.AspNetCore.Mvc;
using PersonaWatch.Application.Abstraction;

namespace PersonaWatch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NewsController : ControllerBase
{
    private readonly INewsContent _newsContentService;
    public NewsController(INewsContent newsContentService)
    {
        _newsContentService = newsContentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetNews(
        [FromQuery] string? search,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo)
    {
        var query = await _newsContentService.GetAllNewsContents();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(n => n.SearchKeyword == search);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(n => n.PublishDate >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(n => n.PublishDate <= dateTo.Value);
        }

        var newsList = query
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
            });

        return Ok(newsList);
    }


    [HttpGet("search-keywords")]
    public async Task<IActionResult> GetSearchKeywords()
    {
        try
        {
            var keywords = await _newsContentService.GetAllSearchKeywords();

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
