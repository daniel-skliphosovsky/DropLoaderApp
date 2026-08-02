using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MediaHub.Services.Downloaders;

/// <summary>
/// Dailymotion video downloader. The video id lives in the URL itself
/// (dailymotion.com/video/&lt;id&gt; or dai.ly/&lt;id&gt;); the public player
/// metadata endpoint returns JSON with direct MP4 streams (progressive) and
/// HLS. Only the direct MP4 entries are used; no login needed.
/// </summary>
public sealed class DailymotionDownloader : ScrapeDownloader
{
    private static readonly Regex DaiLyIdRegex = new(
        @"dai\.ly/(?<id>[a-z0-9]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PathIdRegex = new(
        @"/(?:embed/)?video/(?<id>[a-z0-9]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private const string MetadataEndpoint = "https://www.dailymotion.com/player/metadata/video/{0}";

    public DailymotionDownloader(HttpClient http) : base(http) { }

    public override string PlatformName => Loc.Get(LocKeys.PlatformDailymotion);

    protected override string NoStreamError => Loc.Get(LocKeys.ErrDailymotionNoStream);

    public override bool CanHandle(string url) =>
        UrlHelpers.UrlBelongsTo(url, "dailymotion.com", "dai.ly");

    protected override async Task<string> FetchPageAsync(string url, CancellationToken ct)
    {
        var id = ExtractVideoId(url);
        if (id is null)
            throw new HttpRequestException(Loc.Get(LocKeys.ErrVideoNotFound), null, HttpStatusCode.NotFound);

        using var request = new HttpRequestMessage(HttpMethod.Get, string.Format(MetadataEndpoint, id));
        request.Headers.UserAgent.ParseAdd(UserAgent);
        request.Headers.Referrer = new Uri("https://www.dailymotion.com/");

        using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new HttpRequestException(Loc.Get(LocKeys.ErrVideoNotFound), null, HttpStatusCode.NotFound);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    protected override IEnumerable<(string Quality, string Url)> ExtractStreams(string html)
    {
        var streams = new List<(string, string)>();
        try
        {
            using var doc = JsonDocument.Parse(html);
            var root = doc.RootElement;
            if (root.TryGetProperty("error", out _))
                return streams;

            if (!root.TryGetProperty("qualities", out var qualities) &&
                (!root.TryGetProperty("video", out var video) || !video.TryGetProperty("qualities", out qualities)))
                return streams;

            foreach (var quality in qualities.EnumerateObject())
            {
                if (quality.Value.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var format in quality.Value.EnumerateArray())
                {
                    if (format.TryGetProperty("type", out var type) &&
                        type.GetString() is { } mediaType &&
                        mediaType.Contains("mp4", StringComparison.OrdinalIgnoreCase) &&
                        format.TryGetProperty("url", out var urlEl) &&
                        urlEl.GetString() is { } streamUrl)
                    {
                        streams.Add((quality.Name, streamUrl));
                    }
                }
            }
        }
        catch (JsonException)
        {
        }

        return streams;
    }

    protected override string? ExtractTitle(string html) => GetString(html, "title");

    protected override string? ExtractAuthor(string html)
    {
        try
        {
            using var doc = JsonDocument.Parse(html);
            if (doc.RootElement.TryGetProperty("owner", out var owner) && owner.ValueKind == JsonValueKind.Object)
            {
                if (owner.TryGetProperty("screenname", out var name) && name.ValueKind == JsonValueKind.String)
                    return name.GetString();
                if (owner.TryGetProperty("name", out name) && name.ValueKind == JsonValueKind.String)
                    return name.GetString();
                if (owner.TryGetProperty("username", out name) && name.ValueKind == JsonValueKind.String)
                    return name.GetString();
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    protected override string? ExtractThumbnail(string html) => GetString(html, "thumbnail_url");

    protected override long? ExtractDurationSeconds(string html) => GetNumber(html, "duration");

    private static string? GetString(string json, string property)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static long? GetNumber(string json, string property)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty(property, out var value) &&
                   value.ValueKind == JsonValueKind.Number &&
                   value.TryGetInt64(out var number)
                ? number
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ExtractVideoId(string url)
    {
        var match = DaiLyIdRegex.Match(url);
        if (!match.Success)
            match = PathIdRegex.Match(url);
        return match.Success ? match.Groups["id"].Value : null;
    }
}
