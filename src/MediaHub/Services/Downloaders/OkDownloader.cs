using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MediaHub.Services.Downloaders;

/// <summary>
/// OK.ru (Odnoklassniki) video downloader. The desktop page embeds the full
/// movie JSON (title, author, direct MP4 links) as a string in
/// flashvars.metadata inside the HTML-escaped data-options attribute; a plain
/// GET to the old metadataUrl now returns DASH MPD XML instead of JSON, so
/// the metadata is read from the page itself. No login or cookies needed for
/// public videos.
/// </summary>
public sealed class OkDownloader : ScrapeDownloader
{
    // The data-options attribute is HTML-escaped, so its JSON carries no
    // literal quotes and the lazy match stops safely at the closing one.
    private static readonly Regex DataOptionsRegex = new(
        @"data-options=""(?<json>.*?)""",
        RegexOptions.Compiled | RegexOptions.Singleline);

    // Mobile pages (m.ok.ru) embed the movie JSON directly in data-video.
    private static readonly Regex DataVideoRegex = new(
        @"data-video=""(?<json>[^""]+)""",
        RegexOptions.Compiled);

    // Progressive MP4 labels the CDN publishes, in ascending quality order.
    private static readonly Dictionary<string, int> QualityRank = new(StringComparer.OrdinalIgnoreCase)
    {
        ["mobile"] = 10,
        ["lowest"] = 20,
        ["low"] = 30,
        ["sd"] = 40,
        ["hd"] = 50,
        ["fullhd"] = 60
    };

    public OkDownloader(HttpClient http) : base(http) { }

    public override string PlatformName => Loc.Get(LocKeys.PlatformOk);

    protected override string NoStreamError => Loc.Get(LocKeys.ErrOkNoStream);

    public override bool CanHandle(string url) =>
        UrlHelpers.UrlBelongsTo(url, "ok.ru", "m.ok.ru");

    protected override async Task<string> FetchPageAsync(string url, CancellationToken ct)
    {
        string html;
        try
        {
            html = await base.FetchPageAsync(url, ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // The video page itself is gone: deleted or private video.
            throw new HttpRequestException(Loc.Get(LocKeys.ErrVideoNotFound), ex, HttpStatusCode.NotFound);
        }

        // Primary path: the desktop page carries the full movie JSON as a
        // string in flashvars.metadata inside the escaped data-options
        // attribute. HtmlDecode turns \&quot; into \" (valid JSON escaping),
        // the parser then resolves it into a real JSON string. The page has
        // several data-options attributes, so try each of them.
        foreach (Match optionsMatch in DataOptionsRegex.Matches(html))
        {
            if (TryReadMetadataJson(WebUtility.HtmlDecode(optionsMatch.Groups["json"].Value)) is { } metadata)
                return metadata;
        }

        // Fallback: mobile pages embed the movie JSON directly in data-video.
        var dataVideo = DataVideoRegex.Match(html);
        if (dataVideo.Success)
            return WebUtility.HtmlDecode(dataVideo.Groups["json"].Value);

        // Nothing usable: stream extraction fails with the platform no-stream
        // error instead of a raw HTML error.
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

    /// <summary>
    /// OK.ru names its streams ("mobile", "sd", "hd", "fullhd") instead of
    /// using resolutions; map them to an ascending rank so the best available
    /// quality is picked instead of the first one listed.
    /// </summary>
    protected override int ParseQuality(string quality)
    {
        if (QualityRank.TryGetValue(quality.Trim(), out var rank))
            return rank;
        return base.ParseQuality(quality);
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

        // Direct progressive mp4s are served from signed okcdn URLs
        // (vd*.okcdn.ru/?expires=...&sig=...) with no .mp4 extension, so match
        // on the CDN host as well as the file extension.
        var host = uri.Host.ToLowerInvariant();
        return host == "okcdn.ru" || host.EndsWith(".okcdn.ru", StringComparison.Ordinal) ||
               host == "mycdn.me" || host.EndsWith(".mycdn.me", StringComparison.Ordinal) ||
               uri.AbsolutePath.ToLowerInvariant().EndsWith(".mp4");
    }

    /// <summary>
    /// Reads flashvars.metadata out of a decoded data-options JSON. The
    /// metadata is normally a JSON string; some pages embed it as an object
    /// directly.
    /// </summary>
    private static string? TryReadMetadataJson(string dataOptionsJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(dataOptionsJson);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("flashvars", out var flashvars) &&
                flashvars.ValueKind == JsonValueKind.Object &&
                flashvars.TryGetProperty("metadata", out var metadata))
            {
                return metadata.ValueKind == JsonValueKind.String
                    ? metadata.GetString()
                    : metadata.ValueKind == JsonValueKind.Object
                        ? metadata.GetRawText()
                        : null;
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }
}
