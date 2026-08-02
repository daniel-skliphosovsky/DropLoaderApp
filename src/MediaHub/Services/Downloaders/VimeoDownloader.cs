using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MediaHub.Services.Downloaders;

/// <summary>
/// Vimeo video downloader. The video id is resolved through the public oEmbed
/// endpoint (no key), then the player config returns JSON with direct MP4
/// files; the highest progressive one is downloaded. No login needed, but
/// Vimeo rate-limits by IP, so every request is delayed and 429s are retried.
/// </summary>
public sealed class VimeoDownloader : ScrapeDownloader
{
    private const string OEmbedEndpoint = "https://vimeo.com/api/oembed.json?url=";
    private const string ConfigEndpoint = "https://player.vimeo.com/video/{0}/config";

    // Delay before each request: keeps us under Vimeo's IP-based rate limit.
    private const int RequestDelayMs = 400;
    private const int MaxRetries = 2;

    private static readonly Regex VideoIdRegex = new(
        @"vimeo\.com/(?:video/)?(?<id>\d+)",
        RegexOptions.Compiled);

    public VimeoDownloader(HttpClient http) : base(http) { }

    public override string PlatformName => Loc.Get(LocKeys.PlatformVimeo);

    protected override string NoStreamError => Loc.Get(LocKeys.ErrVimeoNoStream);

    public override bool CanHandle(string url) =>
        UrlHelpers.UrlBelongsTo(url, "vimeo.com", "player.vimeo.com");

    protected override async Task<string> FetchPageAsync(string url, CancellationToken ct)
    {
        await Task.Delay(RequestDelayMs, ct);
        var oembed = await GetJsonWithRetryAsync(OEmbedEndpoint + Uri.EscapeDataString(url), ct);

        var id = ExtractVideoIdFromJson(oembed) ?? ExtractVideoIdFromUrl(url);
        if (id is null)
            throw new HttpRequestException(Loc.Get(LocKeys.ErrVideoNotFound), null, HttpStatusCode.NotFound);

        await Task.Delay(RequestDelayMs, ct);
        return await GetJsonWithRetryAsync(string.Format(ConfigEndpoint, id), ct);
    }

    protected override IEnumerable<(string Quality, string Url)> ExtractStreams(string html)
    {
        var streams = new List<(string, string)>();
        try
        {
            using var doc = JsonDocument.Parse(html);
            var root = doc.RootElement;
            if (!root.TryGetProperty("request", out var request) ||
                !request.TryGetProperty("files", out var files) ||
                !files.TryGetProperty("progressive", out var progressive) ||
                progressive.ValueKind != JsonValueKind.Array)
                return streams;

            foreach (var item in progressive.EnumerateArray())
            {
                if (item.TryGetProperty("url", out var urlEl) && urlEl.GetString() is { } streamUrl &&
                    item.TryGetProperty("quality", out var qEl) && qEl.ValueKind == JsonValueKind.String)
                {
                    streams.Add((qEl.GetString()!, streamUrl));
                }
            }
        }
        catch (JsonException)
        {
        }

        return streams;
    }

    protected override string? ExtractTitle(string html) => GetVideoString(html, "title");

    protected override string? ExtractAuthor(string html)
    {
        try
        {
            using var doc = JsonDocument.Parse(html);
            if (doc.RootElement.TryGetProperty("video", out var video) &&
                video.TryGetProperty("owner", out var owner) && owner.ValueKind == JsonValueKind.Object &&
                owner.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
                return name.GetString();
        }
        catch (JsonException)
        {
        }

        return null;
    }

    protected override string? ExtractThumbnail(string html)
    {
        try
        {
            using var doc = JsonDocument.Parse(html);
            if (doc.RootElement.TryGetProperty("video", out var video) &&
                video.TryGetProperty("thumbs", out var thumbs) && thumbs.ValueKind == JsonValueKind.Object)
            {
                string? best = null;
                var bestWidth = 0;
                foreach (var prop in thumbs.EnumerateObject())
                {
                    if (int.TryParse(prop.Name, out var width) && width > bestWidth &&
                        prop.Value.ValueKind == JsonValueKind.String)
                    {
                        bestWidth = width;
                        best = prop.Value.GetString();
                    }
                }

                return best;
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    protected override long? ExtractDurationSeconds(string html) => GetVideoNumber(html, "duration");

    private async Task<string> GetJsonWithRetryAsync(string endpoint, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.UserAgent.ParseAdd(UserAgent);
            request.Headers.Referrer = new Uri("https://player.vimeo.com/");

            using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt < MaxRetries)
            {
                await Task.Delay(TimeSpan.FromSeconds(1 + attempt), ct);
                continue;
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                throw new HttpRequestException(Loc.Get(LocKeys.ErrVimeoRateLimited), null, HttpStatusCode.TooManyRequests);
            if (response.StatusCode == HttpStatusCode.NotFound)
                throw new HttpRequestException(Loc.Get(LocKeys.ErrVideoNotFound), null, HttpStatusCode.NotFound);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(ct);
        }
    }

    private static string? ExtractVideoIdFromJson(string oembed)
    {
        try
        {
            using var doc = JsonDocument.Parse(oembed);
            if (doc.RootElement.TryGetProperty("video_id", out var id) && id.ValueKind == JsonValueKind.Number &&
                id.TryGetInt64(out var number))
                return number.ToString();
        }
        catch (JsonException)
        {
        }

        return null;
    }

    private static string? ExtractVideoIdFromUrl(string url)
    {
        var match = VideoIdRegex.Match(url);
        return match.Success ? match.Groups["id"].Value : null;
    }

    private static string? GetVideoString(string json, string property)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("video", out var video) &&
                video.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String)
                return value.GetString();
        }
        catch (JsonException)
        {
        }

        return null;
    }

    private static long? GetVideoNumber(string json, string property)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("video", out var video) &&
                video.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number &&
                value.TryGetInt64(out var number))
                return number;
        }
        catch (JsonException)
        {
        }

        return null;
    }
}
