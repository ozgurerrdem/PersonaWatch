using System.Security.Cryptography;
using System.Text;

namespace PersonaWatch.Application.Common.Helpers;

public static class HelperService
{
    public static string ComputeMd5(string input)
    {
        using var md5 = MD5.Create();
        var bytes = Encoding.UTF8.GetBytes(input.ToLowerInvariant().Trim());
        var hash = md5.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }

    public static string NormalizeUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return "";

        try
        {
            var uri = new UriBuilder(url)
            {
                Scheme = "https",
                Port = -1
            };

            var host = uri.Host.Replace("www.", "").Replace("m.", "");
            uri.Host = host;

            return uri.Uri.AbsoluteUri.TrimEnd('/');
        }
        catch
        {
            return url;
        }
    }
}
