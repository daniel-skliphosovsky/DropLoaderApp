using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MediaHub.Services.Downloaders;

/// <summary>
/// OK.ru (Odnoklassniki) video downloader. The desktop page embeds a JSON
/// data-options attribute carrying a metadataUrl; a plain GET to that URL
/// returns the movie JSON with direct MP4 links on the mycdn CDN (up to
/// 1080p). No login or cookies needed for public videos.
/// </summary>
public sealed class OkDownloader : ScrapeDownloader
{
    private static readonly Regex MetadataUrlRegex = new(
        @"metadataUrl""\s*:\s*""(?<url>https?:\\?/\\?/[^""]+)""",
        RegexOptions.Compiled);

    // The data-options attribute is HTML-escaped, so its JSON carries no
    // literal quotes and the lazy match stops safely at the closing one.
    private static readonly Regex DataOptionsRegex = new(
        @"data-options=""(?<json>.*?)""",
        RegexOptions.Compiled | RegexOptions.Singleline);

    // Mobile pages (m.ok.ru) embed the movie JSON directly in data-video.
    private static readonly Regex DataVideoRegex = new(
        @"data-video=""(?<json>[^""]+)""",
        RegexOptions.Compiled);

    public OkDownloader(HttpClient http) : base(http) { }

    public override string PlatformName => Loc.Get(LocKeys.PlatformOk);

    protected override string NoStreamError => Loc.Get(LocKeys.ErrOkNoStream);

    public override bool CanHandle(string url) =>
        UrlHelpers.UrlBelongsTo(url, "ok.ru", "m.ok.ru");

    protected override async Task<string> FetchPageAsync(string url, CancellationToken ct)
    {
        var html = await base.FetchPageAsync(url, ct);

        // The metadataUrl lives inside the escaped data-options JSON; try the
        // raw page first (some variants are not escaped) and the decoded
        // attribute second.
        var metadataMatch = MetadataUrlRegex.Match(html);
        if (!metadataMatch.Success)
        {
            var optionsMatch = DataOptionsRegex.Match(html);
            if (optionsMatch.Success)
                metadataMatch = MetadataUrlRegex.Match(WebUtility.HtmlDecode(optionsMatch.Groups["json"].Value));
        }

        if (metadataMatch.Success)
        {
            var metadataUrl = Unescape(metadataMatch.Groups["url"].Value);
            if (!IsAllowedMetadataHost(metadataUrl))
                throw new HttpRequestException(Loc.Get(LocKeys.ErrVideoNotFound), null, HttpStatusCode.NotFound);

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, metadataUrl);
                request.Headers.UserAgent.ParseAdd(UserAgent);
                if (Uri.TryCreate(url, UriKind.Absolute, out var referer))
                    request.Headers.Referrer = referer;

                using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync(ct);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // 404 from the metadata endpoint: no payload for this video,
                // fall through to the inline data-video payload.
            }
            catch (HttpRequestException ex)
            {
                // 403/5xx metadata failures are not "video not found"; surface
                // them instead of silently falling back.
                throw new HttpRequestException(Loc.Get(LocKeys.ErrOkNoStream), ex, ex.StatusCode);
            }
        }

        var dataVideo = DataVideoRegex.Match(html);
        if (dataVideo.Success)
            return WebUtility.HtmlDecode(dataVideo.Groups["json"].Value);

        // Nothing usable: keep the page HTML; stream extraction fails with the
        // platform no-stream error instead of a raw HTML error.
        return html;
    }

    protected override IEnumerable<(string Quality, string Url)> ExtractStreams(string html)
    {
        var streams = new List<(string, string)>();
        try
        {
            using var doc = JsonDocument.Parse(html);
            WalkStreams(doc.RootElement, null, streams);
        }
        catch (JsonException)
        {
        }

        return streams;
    }

    /// <summary>
    /// Recursively collects every "url" that points at a real MP4. Quality
    /// comes from a "name"/"quality" sibling when present, otherwise from the
    /// dict key that holds the url (e.g. "videos": {"1080": {"url": ...}}).
    /// </summary>
    private static void WalkStreams(JsonElement element, string? parentKey, List<(string Quality, string Url)> streams)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("url", out var urlEl) &&
                urlEl.ValueKind == JsonValueKind.String &&
                IsVideoUrl(urlEl.GetString() ?? string.Empty))
            {
                var quality = element.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String
                    ? nameEl.GetString()
                    : element.TryGetProperty("quality", out var qEl) && qEl.ValueKind == JsonValueKind.String
                        ? qEl.GetString()
                        : parentKey;
                streams.Add((quality ?? string.Empty, urlEl.GetString()!));
                return;
            }

            foreach (var prop in element.EnumerateObject())
                WalkStreams(prop.Value, prop.Name, streams);
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                WalkStreams(item, parentKey, streams);
        }
    }

    protected override string? ExtractTitle(string html) => FindString(html, "title");

    protected override string? ExtractAuthor(string html)
    {
        var authorName = FindString(html, "authorName");
        if (authorName is not null)
            return authorName;

        // Some payloads carry an "author" object with a "name" instead.
        return FindAuthorName(html);
    }

    protected override string? ExtractThumbnail(string html)
        => FindString(html, "pic") ?? FindString(html, "image");

    protected override long? ExtractDurationSeconds(string html) => FindNumber(html, "duration");

    private static string? FindAuthorName(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return FindAuthorName(doc.RootElement);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? FindAuthorName(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (prop.Name == "author" && prop.Value.ValueKind == JsonValueKind.Object &&
                    prop.Value.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
                    return name.GetString();
                if (FindAuthorName(prop.Value) is { } found)
                    return found;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                if (FindAuthorName(item) is { } found)
                    return found;
        }

        return null;
    }

    private static string? FindString(string json, string property)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return FindString(doc.RootElement, property);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static long? FindNumber(string json, string property)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return FindNumber(doc.RootElement, property);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? FindString(JsonElement element, string property)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (prop.Name == property && prop.Value.ValueKind == JsonValueKind.String)
                    return prop.Value.GetString();
                if (FindString(prop.Value, property) is { } found)
                    return found;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                if (FindString(item, property) is { } found)
                    return found;
        }

        return null;
    }

    private static long? FindNumber(JsonElement element, string property)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (prop.Name == property && prop.Value.ValueKind == JsonValueKind.Number &&
                    prop.Value.TryGetInt64(out var value))
                    return value;
                if (FindNumber(prop.Value, property) is { } found)
                    return found;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                if (FindNumber(item, property) is { } found)
                    return found;
        }

        return null;
    }

    private static bool IsVideoUrl(string url)
    {
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
            !Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var path = uri.AbsolutePath.ToLowerInvariant();
        return path.EndsWith(".mp4") || uri.Host.Contains("mycdn.me", StringComparison.OrdinalIgnoreCase);
    }

    private static string Unescape(string url) =>
        WebUtility.HtmlDecode(url
            .Replace("\\/", "/")
            .Replace("\\u0026", "&")
            .Replace("\\x26", "&"));

    /// <summary>
    /// The metadata endpoint may only live on ok.ru or the ok CDNs; a tampered
    /// page must not point the client at an arbitrary host.
    /// </summary>
    private static bool IsAllowedMetadataHost(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;
        var host = uri.Host.ToLowerInvariant();
        return host is "ok.ru" or "mycdn.me" or "okcdn.ru" ||
               host.EndsWith(".ok.ru", StringComparison.Ordinal) ||
               host.EndsWith(".mycdn.me", StringComparison.Ordinal) ||
               host.EndsWith(".okcdn.ru", StringComparison.Ordinal);
    }
}
