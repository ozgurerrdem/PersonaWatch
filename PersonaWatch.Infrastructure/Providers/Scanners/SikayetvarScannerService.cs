using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using PersonaWatch.Application.Abstraction;
using PersonaWatch.Application.Common.Helpers;

namespace PersonaWatch.Infrastructure.Providers.Scanners;

public class SikayetvarScannerService : IScanner
{
    public string Source => "ŞikayetvarSelenium";
    private readonly string _userAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";

    public async Task<List<Domain.Entities.NewsContent>> ScanAsync(string searchKeyword)
    {
        var results = new List<Domain.Entities.NewsContent>();
        if (string.IsNullOrWhiteSpace(searchKeyword))
            return results;

        var cleaned = Regex.Replace(searchKeyword.Trim(), @"\s+", "-");
        var searchUrl = $"https://www.sikayetvar.com/{cleaned}";

        var options = new ChromeOptions();
        options.AddArgument("--headless=new");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument($"--user-agent={_userAgent}");
        options.AddArgument("--disable-blink-features=AutomationControlled");
        options.AddExcludedArgument("enable-automation");

        using var driver = new ChromeDriver(options);
        driver.Navigate().GoToUrl(searchUrl);

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        await Task.Delay(800);
        try { wait.Until(d => d.FindElements(By.CssSelector("article.card-v2")).Count > 0); }
        catch { /* sonuç olmayabilir */ }

        // Lazy-load için biraz kaydır
        ((IJavaScriptExecutor)driver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
        await Task.Delay(600);

        var entries = driver.FindElements(By.CssSelector("article.card-v2"));
        foreach (var entry in entries)
        {
            try
            {
                // Özet
                var commentEl = SafeFind(entry, By.CssSelector("p.complaint-description"));
                var summary = CleanText(commentEl?.Text);
                if (string.IsNullOrWhiteSpace(summary))
                    continue;

                // Başlık
                var titleAnchor = SafeFind(entry, By.CssSelector("h2.complaint-title a"));
                string title = CleanText(titleAnchor?.Text);
                if (string.IsNullOrWhiteSpace(title))
                    title = CleanText(SafeFind(entry, By.CssSelector("h2.complaint-title"))?.Text);
                if (string.IsNullOrWhiteSpace(title))
                    title = "Şikayetvar Kaydı";

                // URL
                string entryUrl = CleanAttr(titleAnchor, "href");
                if (string.IsNullOrWhiteSpace(entryUrl))
                {
                    // Fallback: data-url (base64)
                    var dataUrl = CleanAttr(commentEl, "data-url");
                    if (!string.IsNullOrWhiteSpace(dataUrl))
                    {
                        try
                        {
                            var bytes = Convert.FromBase64String(dataUrl);
                            var decodedPath = Encoding.UTF8.GetString(bytes);
                            entryUrl = "https://www.sikayetvar.com" + decodedPath;
                        }
                        catch { /* yoksay */ }
                    }
                }
                if (string.IsNullOrWhiteSpace(entryUrl))
                    continue;

                var normalizedUrl = HelperService.NormalizeUrl(entryUrl);

                // Publisher (marka/kurum adı)
                string publisher =
                    CleanText(SafeFind(entry, By.CssSelector(".profile-details .company-name"))?.Text);
                if (string.IsNullOrWhiteSpace(publisher))
                    publisher = CleanText(SafeFind(entry, By.CssSelector(".profile-details a"))?.Text);

                // PublishDate (listede gördüğün mutlak/relatif zaman)
                var publishDate = ParsePublishDateFromEntry(entry) ?? DateTime.UtcNow;

                // ViewCount
                var viewCount = ParseViewCount(entry);

                // Opsiyonel: yorum/beğeni/beğenmeme (listede çoğu kez yok; varsa yakalar)
                var (likeCount, dislikeCount, commentCount) = ParseReactions(entry);

                var contentHash = HelperService.ComputeMd5(summary + normalizedUrl);

                results.Add(new Domain.Entities.NewsContent
                {
                    Id = Guid.NewGuid(),
                    Title = title,
                    Summary = summary,
                    Url = entryUrl,
                    Platform = "Şikayetvar",
                    Publisher = publisher,
                    PublishDate = publishDate,            // artık gerçek zaman
                    CreatedDate = DateTime.UtcNow,
                    CreatedUserName = "system",
                    RecordStatus = 'A',
                    SearchKeyword = searchKeyword,
                    ContentHash = contentHash,
                    Source = Source,
                    ViewCount = viewCount,
                    LikeCount = likeCount,
                    DislikeCount = dislikeCount,
                    CommentCount = commentCount
                });
            }
            catch
            {
                // kart bazında sorun varsa atla
                continue;
            }
        }

        return results;
    }

    // ---------- Helpers ----------

    private static IWebElement? SafeFind(ISearchContext ctx, By by)
    {
        try { return ctx.FindElement(by); } catch { return null; }
    }

    private static string CleanText(string? s)
        => string.IsNullOrWhiteSpace(s) ? string.Empty : Regex.Replace(s.Trim(), @"\s+", " ");

    private static string CleanAttr(IWebElement? el, string attr)
    {
        try
        {
            var v = el?.GetAttribute(attr);
            return string.IsNullOrWhiteSpace(v) ? string.Empty : v.Trim();
        }
        catch { return string.Empty; }
    }

    private static TimeZoneInfo IstanbulTz =>
        // Linux & macOS: "Europe/Istanbul", Windows: "Turkey Standard Time"
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time")
            : TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    private static DateTime ToUtcFromIstanbul(DateTime localUnspecified)
        => TimeZoneInfo.ConvertTimeToUtc(localUnspecified, IstanbulTz);

    private static DateTime NowIstanbul()
        => TimeZoneInfo.ConvertTime(DateTime.UtcNow, IstanbulTz);

    private static DateTime? ParsePublishDateFromEntry(IWebElement entry)
    {
        // .post-time .time: "23 Eylül 18:25" | "23 Eylül 2024 18:25" | "bugün 09:24" | "dün 13:19"
        // Bazen "x saat/dk önce" de olabilir.
        var raw = CleanText(SafeFind(entry, By.CssSelector(".post-time .time"))?.GetAttribute("title"));
        if (string.IsNullOrWhiteSpace(raw))
            raw = CleanText(SafeFind(entry, By.CssSelector(".post-time .time"))?.Text);
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var tr = new CultureInfo("tr-TR");
        var nowLocal = NowIstanbul();

        // bugün / dün
        var todayM = Regex.Match(raw, @"\bbugün\b(?:\s+(\d{1,2}):(\d{2}))?", RegexOptions.IgnoreCase);
        if (todayM.Success)
        {
            var h = todayM.Groups[1].Success ? int.Parse(todayM.Groups[1].Value, tr) : 0;
            var m = todayM.Groups[2].Success ? int.Parse(todayM.Groups[2].Value, tr) : 0;
            var local = new DateTime(nowLocal.Year, nowLocal.Month, nowLocal.Day, h, m, 0, DateTimeKind.Unspecified);
            return ToUtcFromIstanbul(local);
        }

        var yesterdayM = Regex.Match(raw, @"\bdün\b(?:\s+(\d{1,2}):(\d{2}))?", RegexOptions.IgnoreCase);
        if (yesterdayM.Success)
        {
            var d = nowLocal.AddDays(-1);
            var h = yesterdayM.Groups[1].Success ? int.Parse(yesterdayM.Groups[1].Value, tr) : 0;
            var m = yesterdayM.Groups[2].Success ? int.Parse(yesterdayM.Groups[2].Value, tr) : 0;
            var local = new DateTime(d.Year, d.Month, d.Day, h, m, 0, DateTimeKind.Unspecified);
            return ToUtcFromIstanbul(local);
        }

        // relatif: "x dk/saat/gün/hafta/ay/yıl önce"
        var rel = Regex.Match(raw, @"(\d+)\s*(dk|dakika|saat|gün|hafta|ay|yıl)\s*önce", RegexOptions.IgnoreCase);
        if (rel.Success)
        {
            var val = int.Parse(rel.Groups[1].Value, tr);
            var unit = rel.Groups[2].Value.ToLower(tr);
            var delta = unit switch
            {
                "dk" or "dakika" => TimeSpan.FromMinutes(val),
                "saat" => TimeSpan.FromHours(val),
                "gün" => TimeSpan.FromDays(val),
                "hafta" => TimeSpan.FromDays(7 * val),
                "ay" => TimeSpan.FromDays(30 * val),
                "yıl" => TimeSpan.FromDays(365 * val),
                _ => TimeSpan.Zero
            };
            var guessLocal = nowLocal - delta;
            return ToUtcFromIstanbul(new DateTime(
                guessLocal.Year, guessLocal.Month, guessLocal.Day,
                guessLocal.Hour, guessLocal.Minute, guessLocal.Second, DateTimeKind.Unspecified));
        }

        // absolute: "23 Eylül 18:25" veya "23 Eylül 2023 18:25" (yıl yoksa nowLocal.Year)
        var mAbs = Regex.Match(raw,
            @"(?<d>\d{1,2})\s+(?<mon>[A-Za-zÇĞİÖŞÜçğıöşü]+)(?:\s+(?<y>\d{4}))?\s+(?<hh>\d{1,2}):(?<mm>\d{2})");
        if (mAbs.Success)
        {
            int day = int.Parse(mAbs.Groups["d"].Value, tr);
            string monName = mAbs.Groups["mon"].Value;
            int year = mAbs.Groups["y"].Success ? int.Parse(mAbs.Groups["y"].Value, tr) : nowLocal.Year;

            // Ay adını sayıya çevir
            int month = DateTime.ParseExact($"01 {monName} {year}", "dd MMMM yyyy", tr).Month;

            int hour = int.Parse(mAbs.Groups["hh"].Value, tr);
            int minute = int.Parse(mAbs.Groups["mm"].Value, tr);

            var local = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified);
            return ToUtcFromIstanbul(local);
        }

        // son çare: TryParse (tr-TR)
        if (DateTime.TryParse(raw, tr, DateTimeStyles.AssumeLocal, out var localParsed))
            return ToUtcFromIstanbul(DateTime.SpecifyKind(localParsed, DateTimeKind.Unspecified));

        return null;
    }

    private static int ParseViewCount(IWebElement entry)
    {
        try
        {
            // .post-time içinde tarihten sonra gelen son sayı görüntülenmedir (ör. "23 Eylül 18:25 380")
            var postTime = SafeFind(entry, By.CssSelector(".post-time"));
            var text = CleanText(postTime?.Text);
            if (string.IsNullOrWhiteSpace(text)) return 0;

            var nums = Regex.Matches(text, @"\d+");
            if (nums.Count > 0 && int.TryParse(nums[^1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var v))
                return v;
        }
        catch { }
        return 0;
    }

    private static (int like, int dislike, int comment) ParseReactions(IWebElement entry)
    {
        // Listede çoğu kez yok; varsa metinden yakalamayı dener.
        int like = 0, dislike = 0, comment = 0;
        try
        {
            var full = CleanText(entry.Text);
            var likeM = Regex.Match(full, @"(\d[\d\.\s]*)\s*beğeni", RegexOptions.IgnoreCase);
            if (likeM.Success) like = ParseIntLoose(likeM.Groups[1].Value);

            var dislikeM = Regex.Match(full, @"(\d[\d\.\s]*)\s*beğenmeme", RegexOptions.IgnoreCase);
            if (dislikeM.Success) dislike = ParseIntLoose(dislikeM.Groups[1].Value);

            var commentM = Regex.Match(full, @"(\d[\d\.\s]*)\s*yorum", RegexOptions.IgnoreCase);
            if (commentM.Success) comment = ParseIntLoose(commentM.Groups[1].Value);
        }
        catch { }
        return (like, dislike, comment);
    }

    private static int ParseIntLoose(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0;
        var digits = Regex.Replace(s, @"[^\d]", "");
        return int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var v) ? v : 0;
    }
}
