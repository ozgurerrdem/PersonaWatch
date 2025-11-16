using System.Text.RegularExpressions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using PersonaWatch.Application.Abstraction;
using PersonaWatch.Application.Common.Helpers;
 
namespace PersonaWatch.Infrastructure.Providers.Scanners;

public class EksiScannerService : IScanner
{
    public string Source => "EkşiSelenium";
    private readonly string _userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";

    public async Task<List<Domain.Entities.NewsContent>> ScanAsync(string searchKeyword)
    {
        var results = new List<Domain.Entities.NewsContent>();
        if (string.IsNullOrWhiteSpace(searchKeyword))
            return results;

        var cleaned = Regex.Replace(searchKeyword.Trim(), @"\s+", "+");
        var searchUrl = $"https://eksisozluk.com/?q={cleaned}";

        var options = new ChromeOptions();
        options.AddArgument("--headless=new");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument($"--user-agent={_userAgent}");

        using var driver = new ChromeDriver(options);

        driver.Navigate().GoToUrl("https://eksisozluk.com/");
        await Task.Delay(1500);

        var cookies = driver.Manage().Cookies.AllCookies;
        var iqCookie = cookies.FirstOrDefault(c => c.Name == "iq");
        Console.WriteLine("iq cookie: " + iqCookie?.Value);

        driver.Navigate().GoToUrl(searchUrl);
        await Task.Delay(1500);

        var pageSource = driver.PageSource;

        var titleMatch = Regex.Match(pageSource, @"'etitle':\s*'([^']+)'");
        var idMatch = Regex.Match(pageSource, @"'econtentid':\s*'([^']+)'");
        string baslikUrl = null;

        if (titleMatch.Success && idMatch.Success)
        {
            var etitle = titleMatch.Groups[1].Value;
            var econtentid = idMatch.Groups[1].Value;
            baslikUrl = $"https://eksisozluk.com/{etitle}--{econtentid}";
        }
        else
        {
            var baslikNode = driver.FindElements(By.CssSelector("ul.topic-list li a"))
                .FirstOrDefault(e => Regex.IsMatch(e.GetAttribute("href") ?? "", @"--\d+$"));

            if (baslikNode != null)
                baslikUrl = "https://eksisozluk.com" + baslikNode.GetAttribute("href");
            else
                return results;
        }

        var lastPageMatch = Regex.Match(pageSource, @"<a href=""\?p=(\d+)""[^>]*class=""last""[^>]*>");
        int lastPage = lastPageMatch.Success && int.TryParse(lastPageMatch.Groups[1].Value, out int page) ? page : 1;

        var targetPages = lastPage > 1 ? new List<int> { lastPage - 1, lastPage } : new List<int> { lastPage };

        foreach (var pageNum in targetPages.Distinct())
        {
            var pageUrl = baslikUrl + (pageNum > 1 ? $"?p={pageNum}" : "");
            driver.Navigate().GoToUrl(pageUrl);
            await Task.Delay(1500);

            var entries = driver.FindElements(By.CssSelector("li[id^='entry-']"));

            foreach (var entry in entries)
            {
                try
                {
                    var contentNode = entry.FindElement(By.CssSelector("div.content"));
                    var entryText = contentNode?.Text.Trim() ?? "";

                    var authorNode = entry.FindElement(By.CssSelector("a.entry-author"));
                    var author = authorNode?.Text.Trim() ?? "anonim";

                    var dateNode = entry.FindElement(By.CssSelector("a.entry-date"));
                    var dateStr = dateNode?.Text.Trim();
                    DateTime publishDate = DateTime.UtcNow;
                    DateTime.TryParse(dateStr, out publishDate);

                    var entryId = entry.GetAttribute("data-id");
                    var entryUrl = $"https://eksisozluk.com/entry/{entryId}";
                    var normalizedUrl = HelperService.NormalizeUrl(entryUrl);
                    var contentHash = HelperService.ComputeMd5(entryText + normalizedUrl);

                    results.Add(new Domain.Entities.NewsContent
                    {
                        Id = Guid.NewGuid(),
                        Title = author,
                        Summary = entryText,
                        Url = entryUrl,
                        Platform = "Ekşi Sözlük",
                        PublishDate = publishDate,
                        CreatedDate = DateTime.UtcNow,
                        CreatedUserName = "system",
                        RecordStatus = 'A',
                        SearchKeyword = searchKeyword,
                        ContentHash = contentHash,
                        Source = Source
                    });
                }
                catch
                {
                    continue;
                }
            }
        }

        return results;
    }
}
