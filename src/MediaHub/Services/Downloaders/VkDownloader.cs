using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MediaHub.Models;
using MediaHub.Services.Interfaces;

namespace MediaHub.Services.Downloaders;

/// <summary>
/// VK video downloader. VK stopped embedding stream URLs in the page HTML
/// (JS-rendered), so this downloader calls the internal al_video.php endpoint
/// the web player itself uses: a POST with act=show returns the direct
/// "urlXXX" mp4 links in the payload. No login needed for public videos;
/// private or region-restricted videos simply have no such keys.
/// </summary>
public sealed class VkDownloader : ScrapeDownloader
{
    private static readonly Regex StreamRegex = new(
        @"""url(?<quality>\d{3,4})""\s*:\s*""(?<url>https?:\\?/\\?/[^""]+)""",
        RegexOptions.Compiled);

    private static readonly Regex AuthorRegex = new(
        @"""md_author""\s*:\s*""(?<author>[^""]+)""",
        RegexOptions.Compiled);

    private static readonly Regex DurationRegex = new(
        @"""duration""\s*:\s*(?<seconds>\d+)",
        RegexOptions.Compiled);

    private static readonly Regex ViewsRegex = new(
        @"""views""\s*:\s*(?<count>\d+)",
        RegexOptions.Compiled);

    // The payload carries likes both flat ("likes":123) and as an object
    // ("likes":{"count":123,"user_likes":0}); try the object form first.
    private static readonly Regex LikesNestedRegex = new(
        @"""likes""\s*:\s*\{[^}]*?""count""\s*:\s*(?<count>\d+)",
        RegexOptions.Compiled);

    private static readonly Regex LikesFlatRegex = new(
        @"""likes""\s*:\s*(?<count>\d+)",
        RegexOptions.Compiled);

    private static readonly Regex DescriptionRegex = new(
        @"""description""\s*:\s*""(?<text>[^""]+)""",
        RegexOptions.Compiled);

    private static readonly Regex ThumbnailRegex = new(
        @"""jpg""\s*:\s*""(?<url>https?:\\?/\\?/[^""]+)""",
        RegexOptions.Compiled);

    private const string ApiEndpoint = "https://vk.com/al_video.php";
    private const string VideoIdPattern = @"\/video(?<oid>-?\d+)_(?<vid>\d+)";

    // VK embeds non-ASCII text (titles, descriptions, authors) as JSON
    // \uXXXX escapes in the payload; decode them before display.
    private static readonly Regex UnicodeEscapeRegex = new(
        @"\\u(?<hex>[0-9a-fA-F]{4})",
        RegexOptions.Compiled);

    static VkDownloader()
    {
        // VK answers in windows-1251 regardless of the Accept-Charset header.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public VkDownloader(HttpClient http) : base(http) { }

    public override string PlatformName => "VK";

    protected override string NoStreamError =>
        "could not extract a video link. The video may be private, age-restricted, or unavailable in your region.";

    public override bool CanHandle(string url) =>
        UrlHelpers.UrlBelongsTo(url, "vk.com", "m.vk.com", "vkvideo.ru", "m.vkvideo.ru");

    protected override string ResolvePageUrl(string url)
    {
        // The API call is the same regardless of the domain, so the original
        // url is used only to extract the video id. Keep it as-is.
        return url;
    }

    protected override async Task<string> FetchPageAsync(string url, CancellationToken ct)
    {
        string? videoId = ExtractVideoId(url);
        if (videoId is null)
            return string.Empty;

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["act"] = "show",
            ["video"] = videoId,
            ["al"] = "1"
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, ApiEndpoint) { Content = content };
        request.Headers.UserAgent.ParseAdd(UserAgent);
        request.Headers.Referrer = new Uri("https://vk.com/");
        request.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");

        using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        byte[] bytes = await response.Content.ReadAsByteArrayAsync(ct);
        string payload = Encoding.GetEncoding("windows-1251").GetString(bytes);

        try
        {
            // The payload is [0, [title, videoBoxHtml, jsTemplates, infoHtml, opts]]
            // wrapped in a "payload" property; some responses are a bare array.
            // Flatten all parts (the trailing opts object included) so the
            // extractors can parse one string; the first entry is the video title.
            using var document = JsonDocument.Parse(payload);
            JsonElement root = document.RootElement;

            JsonElement payloadArr = root.ValueKind == JsonValueKind.Array
                ? root
                : root.TryGetProperty("payload", out JsonElement wrapped) ? wrapped : default;

            if (payloadArr.ValueKind == JsonValueKind.Array
                && payloadArr.GetArrayLength() > 1
                && payloadArr[1].ValueKind == JsonValueKind.Array)
            {
                var builder = new StringBuilder();
                foreach (JsonElement part in payloadArr[1].EnumerateArray())
                {
                    switch (part.ValueKind)
                    {
                        case JsonValueKind.String:
                            builder.Append(part.GetString());
                            break;
                        case JsonValueKind.Object:
                        case JsonValueKind.Array:
                            builder.Append(JsonSerializer.Serialize(part));
                            break;
                        default:
                            continue;
                    }
                    builder.Append('\n');
                }
                return builder.ToString();
            }

            return payload;
        }
        catch (JsonException)
        {
            // HTML error page or otherwise non-JSON body: let the extractors
            // run on the raw payload instead of crashing.
            return payload;
        }
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
        // direct mp4, used only when embedded sources are absent. Only trust
        // them when they point at an actual media file, not at a page.
        var ogUrl = GetMetaProperty(html, "og:video") ?? GetMetaProperty(html, "og:video:url");
        if (IsMediaUrl(ogUrl))
            streams.Add(("best", ogUrl!));

        return streams;
    }

    protected override string? ExtractTitle(string html)
    {
        // First flattened payload entry is the video title.
        string firstLine = html.Split('\n')[0].Trim();
        return firstLine.Length >= 4 ? DecodeUnicode(firstLine) : null;
    }

    protected override string? ExtractAuthor(string html)
    {
        var match = AuthorRegex.Match(html);
        if (match.Success)
            return WebUtility.HtmlDecode(DecodeUnicode(match.Groups["author"].Value));

        // Older payloads only carry the author id; the channel name is absent.
        return null;
    }

    protected override long? ExtractDurationSeconds(string html)
    {
        // Pick the longest duration value, which is the video itself (the
        // payload also embeds durations of recommended videos).
        long? best = null;
        foreach (Match match in DurationRegex.Matches(html))
        {
            if (long.TryParse(match.Groups["seconds"].Value, out long seconds)
                && (best is null || seconds > best))
                best = seconds;
        }
        return best;
    }

    protected override string? ExtractDescription(string html)
    {
        // The flattened payload carries the video description in a
        // "description" key; pick the longest match over recommended videos.
        string? best = null;
        foreach (Match match in DescriptionRegex.Matches(html))
        {
            var text = DecodeUnicode(match.Groups["text"].Value.Trim());
            if (text.Length > 0 && (best is null || text.Length > best.Length))
                best = text;
        }
        return best ?? base.ExtractDescription(html);
    }

    protected override IReadOnlyList<ResourceDetail> BuildDetails(string html)
    {
        var details = base.BuildDetails(html).ToList();

        // Counts repeat once per recommended video in the payload; the video
        // itself is the most popular entry, so keep the largest value.
        var views = MaxCount(ViewsRegex, html);
        if (views > 0)
            details.Add(new ResourceDetail("Views", views.ToString("N0")));

        var likes = MaxCount(LikesNestedRegex, html);
        if (likes == 0)
            likes = MaxCount(LikesFlatRegex, html);
        if (likes > 0)
            details.Add(new ResourceDetail("Likes", likes.ToString("N0")));

        return details;
    }

    private static ulong MaxCount(Regex regex, string html)
    {
        ulong best = 0;
        foreach (Match match in regex.Matches(html))
        {
            if (ulong.TryParse(match.Groups["count"].Value, out ulong value) && value > best)
                best = value;
        }
        return best;
    }

    protected override string? ExtractThumbnail(string html)
    {
        var match = ThumbnailRegex.Match(html);
        return match.Success ? Unescape(match.Groups["url"].Value) : null;
    }

    protected override void ApplyDownloadHeaders(HttpRequestMessage request, string url)
    {
        // The okcdn CDN only serves the mp4 when the request looks like it
        // comes from the VK player.
        request.Headers.TryAddWithoutValidation("Origin", "https://vk.com");
    }

    private static string? ExtractVideoId(string url)
    {
        var match = Regex.Match(url, VideoIdPattern);
        return match.Success ? match.Groups["oid"].Value + "_" + match.Groups["vid"].Value : null;
    }

    private static bool IsMediaUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) ||
            !url.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
            !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
            return false;

        var lower = uri.AbsolutePath.ToLowerInvariant();
        return lower.EndsWith(".mp4") ||
               lower.EndsWith(".m3u8") ||
               lower.EndsWith(".webm") ||
               uri.Host.Contains("vkvideocdn") ||
               uri.Host.Contains("okcdn") ||
               uri.Host.Contains("userapi.com");
    }

    private static string Unescape(string url)
    {
        var value = url
            .Replace("\\/", "/")
            .Replace("\\u0026", "&")
            .Replace("\\x26", "&");

        return WebUtility.HtmlDecode(value);
    }

    /// <summary>
    /// Converts JSON \uXXXX escapes into real characters (e.g. "\u041c" to
    /// Cyrillic "М"). VK serves titles/descriptions/authors escaped this way.
    /// </summary>
    private static string DecodeUnicode(string value) =>
        UnicodeEscapeRegex.Replace(value, match =>
            ((char)Convert.ToInt32(match.Groups["hex"].Value, 16)).ToString());
}
