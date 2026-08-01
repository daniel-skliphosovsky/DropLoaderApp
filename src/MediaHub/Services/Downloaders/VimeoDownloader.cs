using System.Text.Json;
using System.Text.RegularExpressions;
using MediaHub.Services.Interfaces;

namespace MediaHub.Services.Downloaders;

/// <summary>
/// Vimeo scraper. The embed player page (player.vimeo.com) ships the full
/// config with progressive mp4 links inline, no API key or login needed.
/// </summary>
public sealed partial class VimeoDownloader : ScrapeDownloader
{
    public VimeoDownloader(HttpClient http) : base(http) { }

    public override string PlatformName => "Vimeo";

    public override bool CanHandle(string url) =>
        UrlHelpers.UrlBelongsTo(url, "vimeo.com");

    protected override string ResolvePageUrl(string url)
    {
        // Rewrite to the player page, which exposes the stream config that the
        // main site now hides behind a bot check.
        var id = Regex.Match(url, @"vimeo\.com(?:/[^/]*)*/(\d+)").Groups[1].Value;
        return string.IsNullOrEmpty(id) ? url : $"https://player.vimeo.com/video/{id}";
    }

    protected override IEnumerable<(string Quality, string Url)> ExtractStreams(string html)
    {
        using var config = ParsePlayerConfig(html);
        if (config is null ||
            !config.RootElement.TryGetProperty("request", out var request) ||
            !request.TryGetProperty("files", out var files) ||
            !files.TryGetProperty("progressive", out var progressive))
            return [];

        var streams = new List<(string, string)>();
        foreach (var item in progressive.EnumerateArray())
        {
            if (item.TryGetProperty("url", out var url) &&
                item.TryGetProperty("quality", out var quality) &&
                quality.GetString() is { } q && url.GetString() is { Length: > 0 } u)
            {
                streams.Add((q, u));
            }
        }

        return streams;
    }

    protected override string? ExtractTitle(string html) => ReadVideoField(html, "title") ?? base.ExtractTitle(html);

    protected override string? ExtractThumbnail(string html) =>
        ReadVideoField(html, "thumbnail_url") ?? base.ExtractThumbnail(html);

    protected override string? ExtractAuthor(string html)
    {
        using var config = ParsePlayerConfig(html);
        if (config is null ||
            !config.RootElement.TryGetProperty("video", out var video) ||
            !video.TryGetProperty("owner", out var owner) ||
            !owner.TryGetProperty("name", out var name))
            return null;

        return name.GetString();
    }

    protected override long? ExtractDurationSeconds(string html) =>
        ReadVideoField(html, "duration") is { } duration && long.TryParse(duration, out var seconds)
            ? seconds
            : null;

    private static string? ReadVideoField(string html, string field)
    {
        using var config = ParsePlayerConfig(html);
        if (config is null ||
            !config.RootElement.TryGetProperty("video", out var video) ||
            !video.TryGetProperty(field, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };
    }

    private static JsonDocument? ParsePlayerConfig(string html)
    {
        if (!JsonExtractor.TryExtract(html, "window.playerConfig", out var json))
            return null;

        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
