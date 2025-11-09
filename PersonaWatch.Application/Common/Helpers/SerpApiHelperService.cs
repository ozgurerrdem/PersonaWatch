using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PersonaWatch.Application.Common.Helpers;

public static class SerpApiHelperService
{
    // Türkçe relatif üniteler
    private static readonly Dictionary<string, string> TrRelativeUnits = new(StringComparer.OrdinalIgnoreCase)
        {
            { "saniye", "second" }, { "sn", "second" },
            { "dakika", "minute" }, { "dk", "minute" },
            { "saat",   "hour"   },
            { "gün",    "day"    },
            { "hafta",  "week"   },
            { "ay",     "month"  },
            { "yıl",    "year"   }
        };

    // detected_extensions anahtarları (google_videos)
    private static readonly Dictionary<string, string> TrDetectedAgoKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            { "saniye_önce", "second" },
            { "dakika_önce", "minute" },
            { "dk_önce",     "minute" },
            { "saat_önce",   "hour"   },
            { "gün_önce",    "day"    },
            { "hafta_önce",  "week"   },
            { "ay_önce",     "month"  },
            { "yıl_önce",    "year"   },
            { "gun_once",    "day"    },
            { "ay_once",     "month"  }
        };

    // TR ay kısa adları
    private static readonly Dictionary<string, int> TrMonthShort = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Oca", 1 }, { "Şub", 2 }, { "Sub", 2 }, { "Mar", 3 }, { "Nis", 4 },
            { "May", 5 }, { "Haz", 6 }, { "Tem", 7 }, { "Ağu", 8 }, { "Agu", 8 },
            { "Eyl", 9 }, { "Eki",10 }, { "Kas",11 }, { "Ara",12 }
        };

    private static readonly string[] DateFormats =
    {
            // TR
            "d MMM yyyy", "dd MMM yyyy", "d MMMM yyyy", "dd MMMM yyyy",
            "d MMM", "dd MMM", "d MMMM", "dd MMMM",
            // EN
            "MMM d, yyyy", "MMMM d, yyyy", "d MMM yyyy", "dd MMM yyyy"
        };

    private static readonly CultureInfo[] Cultures =
    {
            new CultureInfo("tr-TR"),
            CultureInfo.InvariantCulture,
            new CultureInfo("en-US"),
            new CultureInfo("en-GB")
        };

    private const DateTimeStyles StylesAssumeUtc = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;

    // ---- meta zamanları ----
    public static DateTime GetBaseTimeUtc(JsonElement root)
    {
        if (TryReadSearchMetaTimeUtc(root, "processed_at", out var t1)) return t1;
        if (TryReadSearchMetaTimeUtc(root, "created_at", out var t2)) return t2;
        return DateTime.UtcNow;
    }

    private static bool TryReadSearchMetaTimeUtc(JsonElement root, string prop, out DateTime utc)
    {
        utc = default;
        if (root.TryGetProperty("search_metadata", out var meta) &&
            meta.ValueKind == JsonValueKind.Object &&
            meta.TryGetProperty(prop, out var s) &&
            s.ValueKind == JsonValueKind.String)
        {
            var str = s.GetString();
            if (!string.IsNullOrWhiteSpace(str) && TryParseSerpApiMetaUtc(str!, out var dt))
            {
                utc = dt;
                return true;
            }
        }
        return false;
    }

    private static bool TryParseSerpApiMetaUtc(string s, out DateTime utc)
    {
        return DateTime.TryParseExact(
            s,
            "yyyy-MM-dd HH:mm:ss 'UTC'",
            CultureInfo.InvariantCulture,
            StylesAssumeUtc,
            out utc
        );
    }

    // ---- google_news ----
    public static DateTime? ParseGoogleNewsDate(string? dateField)
    {
        if (string.IsNullOrWhiteSpace(dateField)) return null;

        var formats = new[]
        {
                "MM/dd/yyyy, hh:mm tt, +0000 UTC",
                "M/d/yyyy, h:mm tt, +0000 UTC",
                "MM/dd/yyyy, h:mm tt, +0000 UTC",
                "M/d/yyyy, hh:mm tt, +0000 UTC"
            };

        foreach (var fmt in formats)
        {
            if (DateTime.TryParseExact(dateField, fmt, CultureInfo.InvariantCulture, StylesAssumeUtc, out var dt))
                return dt;
        }

        var trimmed = dateField.Replace(", +0000 UTC", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (DateTime.TryParseExact(trimmed, new[] { "MM/dd/yyyy, hh:mm tt", "M/d/yyyy, h:mm tt" },
                                   CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
        {
            return DateTime.SpecifyKind(local, DateTimeKind.Utc);
        }

        return null;
    }

    // ---- google (web/organic) ----
    public static DateTime? ParseGoogleOrganicDate(JsonElement organicItem, DateTime baseUtc)
    {
        if (organicItem.ValueKind != JsonValueKind.Object) return null;

        if (organicItem.TryGetProperty("date", out var d) && d.ValueKind == JsonValueKind.String)
        {
            var s = d.GetString();
            return ParseDateOrRelative(s, baseUtc);
        }

        return null;
    }

    // ---- google_videos ----
    public static DateTime? ParseGoogleVideosDate(JsonElement videoItem, DateTime baseUtc)
    {
        if (videoItem.ValueKind != JsonValueKind.Object) return null;

        if (TryGetDetectedExtensions(videoItem, out var det))
        {
            if (det.TryGetProperty("date", out var dateStr) && dateStr.ValueKind == JsonValueKind.String)
            {
                var parsed = ParseLocalizedDateString(dateStr.GetString()!);
                if (parsed.HasValue) return EnsureUtc(parsed.Value);
            }

            var ago = ParseDetectedAgo(det, baseUtc);
            if (ago.HasValue) return ago.Value;

            var md = ParseMonthDayFromDetected(det);
            if (md.HasValue)
            {
                var (month, day) = md.Value;
                var year = TryFindYearFromExtensions(videoItem) ?? baseUtc.Year;
                if (TryBuildDate(year, month, day, out var cal))
                    return EnsureUtc(cal);
            }
        }

        foreach (var extStr in EnumerateExtensions(videoItem))
        {
            var parsed = ParseLocalizedDateString(extStr);
            if (parsed.HasValue) return EnsureUtc(parsed.Value);
        }

        return null;
    }

    // ---- ortak yardımcılar ----
    public static DateTime? ParseDateOrRelative(string? s, DateTime baseUtc)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;

        var rel = ParseRelativePhrase(s!, baseUtc);
        if (rel.HasValue) return rel.Value;

        var abs = ParseLocalizedDateString(s!);
        if (abs.HasValue) return EnsureUtc(abs.Value);

        return null;
    }

    private static DateTime? ParseRelativePhrase(string s, DateTime baseUtc)
    {
        s = s.Trim();

        var trMatch = Regex.Match(s, @"(?i)\b(\d+)\s+([a-zçğıöşü_]+)\s+önce\b");
        if (trMatch.Success)
        {
            var val = int.Parse(trMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            var unitRaw = trMatch.Groups[2].Value;
            foreach (var kv in TrRelativeUnits)
            {
                if (unitRaw.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase))
                    return Subtract(baseUtc, kv.Value, val);
            }
        }

        var enMatch = Regex.Match(s, @"(?i)\b(\d+)\s+(seconds?|minutes?|hours?|days?|weeks?|months?|years?)\s+ago\b");
        if (enMatch.Success)
        {
            var val = int.Parse(enMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            var unit = enMatch.Groups[2].Value.ToLowerInvariant();
            unit = unit.EndsWith("s") ? unit[..^1] : unit;
            return Subtract(baseUtc, unit, val);
        }

        return null;
    }

    private static DateTime? Subtract(DateTime baseUtc, string unit, int value)
    {
        return unit switch
        {
            "second" => baseUtc.AddSeconds(-value),
            "minute" => baseUtc.AddMinutes(-value),
            "hour" => baseUtc.AddHours(-value),
            "day" => baseUtc.AddDays(-value),
            "week" => baseUtc.AddDays(-7 * value),
            "month" => baseUtc.AddMonths(-value),
            "year" => baseUtc.AddYears(-value),
            _ => (DateTime?)null
        };
    }

    private static DateTime? ParseLocalizedDateString(string s)
    {
        s = s.Trim();

        foreach (var ci in Cultures)
        {
            if (DateTime.TryParseExact(s, DateFormats, ci, DateTimeStyles.None, out var dt))
                return dt;
        }

        var m = Regex.Match(s, @"(?i)\b(?<d>\d{1,2})\s+(?<mon>[A-Za-zÇĞİÖŞÜçğıöşü]{3,})\.?,?\s*(?<y>\d{4})?\b");
        if (m.Success)
        {
            var day = int.Parse(m.Groups["d"].Value, CultureInfo.InvariantCulture);
            var monStr = m.Groups["mon"].Value;
            var year = m.Groups["y"].Success ? int.Parse(m.Groups["y"].Value, CultureInfo.InvariantCulture) : DateTime.UtcNow.Year;

            if (TryResolveMonth(monStr, out var month) && TryBuildDate(year, month, day, out var dt2))
                return dt2;
        }

        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt3))
            return dt3;

        return null;
    }

    private static bool TryResolveMonth(string token, out int month)
    {
        month = 0;
        if (TrMonthShort.TryGetValue(token, out month)) return true;

        var monthIndex = Array.FindIndex(new[]
        {
                "Ocak","Şubat","Mart","Nisan","Mayıs","Haziran","Temmuz","Ağustos","Eylül","Ekim","Kasım","Aralık"
            }, m => token.StartsWith(m, StringComparison.OrdinalIgnoreCase));
        if (monthIndex >= 0) { month = monthIndex + 1; return true; }

        if (DateTime.TryParseExact(token, new[] { "MMM", "MMMM" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var any))
        {
            month = any.Month;
            return true;
        }

        return false;
    }

    private static bool TryBuildDate(int year, int month, int day, out DateTime dt)
    {
        dt = default;
        try
        {
            dt = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified);
            return true;
        }
        catch { return false; }
    }

    private static DateTime EnsureUtc(DateTime dt)
    {
        return dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
    }

    private static bool TryGetDetectedExtensions(JsonElement videoItem, out JsonElement detected)
    {
        detected = default;
        if (videoItem.TryGetProperty("rich_snippet", out var rs) &&
            rs.ValueKind == JsonValueKind.Object &&
            rs.TryGetProperty("top", out var top) &&
            top.ValueKind == JsonValueKind.Object &&
            top.TryGetProperty("detected_extensions", out var de) &&
            de.ValueKind == JsonValueKind.Object)
        {
            detected = de;
            return true;
        }
        return false;
    }

    private static IEnumerable<string> EnumerateExtensions(JsonElement videoItem)
    {
        if (videoItem.TryGetProperty("rich_snippet", out var rs) &&
            rs.ValueKind == JsonValueKind.Object &&
            rs.TryGetProperty("top", out var top) &&
            top.ValueKind == JsonValueKind.Object &&
            top.TryGetProperty("extensions", out var exts) &&
            exts.ValueKind == JsonValueKind.Array)
        {
            foreach (var e in exts.EnumerateArray())
            {
                if (e.ValueKind == JsonValueKind.String)
                    yield return e.GetString()!;
            }
        }
    }

    private static DateTime? ParseDetectedAgo(JsonElement detected, DateTime baseUtc)
    {
        foreach (var prop in detected.EnumerateObject())
        {
            var name = prop.Name.Trim().ToLowerInvariant();
            if (!TrDetectedAgoKeys.TryGetValue(name, out var unit)) continue;

            var v = prop.Value;
            int amount = 0;

            if (v.ValueKind == JsonValueKind.Number)
            {
                if (!v.TryGetInt32(out amount)) continue;
            }
            else if (v.ValueKind == JsonValueKind.String)
            {
                if (!int.TryParse(v.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out amount))
                    continue;
            }
            else continue;

            return Subtract(baseUtc, unit, amount);
        }
        return null;
    }

    private static (int month, int day)? ParseMonthDayFromDetected(JsonElement detected)
    {
        foreach (var prop in detected.EnumerateObject())
        {
            if (TrMonthShort.ContainsKey(prop.Name) && TryResolveMonth(prop.Name, out var month))
            {
                int day = 0;
                if (prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetInt32(out day) && day is >= 1 and <= 31)
                    return (month, day);
                if (prop.Value.ValueKind == JsonValueKind.String &&
                    int.TryParse(prop.Value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out day) &&
                    day is >= 1 and <= 31)
                    return (month, day);
            }
        }
        return null;
    }

    private static int? TryFindYearFromExtensions(JsonElement videoItem)
    {
        foreach (var ext in EnumerateExtensions(videoItem))
        {
            var y = Regex.Match(ext, @"\b(20\d{2}|19\d{2})\b");
            if (y.Success) return int.Parse(y.Value, CultureInfo.InvariantCulture);
        }
        return null;
    }
}
