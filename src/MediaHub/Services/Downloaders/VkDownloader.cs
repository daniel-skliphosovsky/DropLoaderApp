using System.Net;
using System.Text.RegularExpressions;
using MediaHub.Services.Interfaces;

namespace MediaHub.Services.Downloaders;

/// <summary>
/// VK video scraper. Public videos embed the stream URLs as JSON keys on the
/// mobile page ("url240".."url2160"); no token needed as long as the video is
/// public. Private or region-restricted videos simply have no such keys, in
/// which case a clear error is returned.
/// </summary>
public sealed class VkDownloader : ScrapeDownloader
{
    private static readonly Regex StreamRegex = new(
        @"""url(?<quality>\d{3,4})""\s*:\s*""(?<url>https?:\\?/\\?/[^""]+)""",
        RegexOptions.Compiled);

    public VkDownloader(HttpClient http) : base(http) { }

    public override string PlatformName => "VK";

    protected override string NoStreamError =>
        "could not extract a video link. The video may be private, age-restricted, or unavailable in your region.";

    public override bool CanHandle(string url) =>
        UrlHelpers.UrlBelongsTo(url, "vk.com", "m.vk.com", "vkvideo.ru", "m.vkvideo.ru");

    protected override string ResolvePageUrl(string url)
    {
        // The mobile layout is lighter and still embeds the video data.
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            !Uri.TryCreate("https://" + url, UriKind.Absolute, out uri))
            return url;

        return uri.Host == "m.vk.com"
            ? uri.AbsoluteUri
            : "https://m." + uri.Authority + uri.PathAndQuery;
    }

    protected override IEnumerable<(string Quality, string Url)> ExtractStreams(string html)
    {
        var streams = new List<(string, string)>();
        foreach (Match match in StreamRegex.Matches(html))
        {
            var streamUrl = Unescape(match.Groups["url"].Value);
            if (streamUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                streams.Add((match.Groups["quality"].Value + "p", streamUrl));
        }

        // Fall back to og:video / og:video:url meta tags. VK drops them on
        // some pages (especially vkvideo.ru), but when present they carry the
        // direct mp4, which beats the embedded-json sources above. Only trust
        // them when they point at an actual media file, not at a page.
        var ogUrl = GetMetaProperty(html, "og:video") ?? GetMetaProperty(html, "og:video:url");
        if (IsMediaUrl(ogUrl))
            streams.Add(("best", ogUrl!));

        return streams;
    }

    private static bool IsMediaUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) ||
            !url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return false;

        var uri = new Uri(url, UriKind.Absolute);
        var lower = uri.AbsolutePath.ToLowerInvariant();
        return lower.EndsWith(".mp4") ||
               lower.EndsWith(".m3u8") ||
               lower.EndsWith(".webm") ||
               uri.Host.Contains("vkvideocdn") ||
               uri.Host.Contains("userapi.com");
    }

    protected override long? ExtractDurationSeconds(string html)
    {
        var match = Regex.Match(html, @"""duration""\s*:\s*(?<seconds>\d+)");
        return match.Success && long.TryParse(match.Groups["seconds"].Value, out var seconds)
            ? seconds
            : null;
    }

    protected override string? ExtractAuthor(string html)
    {
        var match = Regex.Match(html,
            @"""author""\s*:\s*\{[^}]*?""(?:name|title)""\s*:\s*""(?<name>[^""]+)""");
        return match.Success ? match.Groups["name"].Value : null;
    }

    private static string Unescape(string url)
    {
        var value = url
            .Replace("\\/", "/")
            .Replace("\\u0026", "&")
            .Replace("\\x26", "&");

        return WebUtility.HtmlDecode(value);
    }
}
