namespace MyBrowser
{
    using System;
    using System.IO;

    public static class BrowserUrlNormalizer
    {
        public static string Normalize(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return "about:blank";
            }

            url = url.Trim();

            if (url == "about:blank" || url.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("file://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("chrome:", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            if (url.StartsWith("C:") || url.StartsWith("D:") || url.StartsWith("E:") || url.StartsWith("F:"))
            {
                var normalized = url.Replace('\\', '/');
                return $"file:///{normalized}";
            }

            if (url.StartsWith("/", StringComparison.Ordinal))
            {
                if (url.StartsWith("//"))
                {
                    return $"file:{url}";
                }
                return $"file://{url}";
            }

            if (url.Contains('.') && !url.Contains(' '))
            {
                if (url.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
                {
                    return "https://" + url;
                }
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttp)
                {
                    return url;
                }
            }

            return url;
        }
    }
}
