using System.Net;
using System.Text.RegularExpressions;
using MediaHub.Models;
using MediaHub.Services.Interfaces;

namespace MediaHub.Services.Downloaders;

/// <summary>
/// Base for downloaders that scrape an HTML page for a direct mp4 link
/// without any API key or login (VK). Metadata comes from the page itself
/// (og tags or embedded JSON), the file is streamed like everywhere else in
/// the app.
/// </summary>
public abstract class ScrapeDownloader : IDownloader
{
    protected const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";

    protected readonly HttpClient Http;

    protected ScrapeDownloader(HttpClient http) => Http = http;

    public abstract string PlatformName { get; }
    public abstract bool CanHandle(string url);

    /// <summary>
    /// Direct media URLs found on the page, with their quality labels
    /// (e.g. "720p" or "1080"). Order does not matter, the best is picked.
    /// </summary>
    protected abstract IEnumerable<(string Quality, string Url)> ExtractStreams(string html);

    protected virtual string ResolvePageUrl(string url) => url;

    protected virtual string? ExtractTitle(string html) => GetMetaProperty(html, "og:title");
    protected virtual string? ExtractThumbnail(string html) => GetMetaProperty(html, "og:image");
    protected virtual string? ExtractAuthor(string html) => null;
    protected virtual string? ExtractDescription(string html) => GetMetaProperty(html, "og:description");
    protected virtual long? ExtractDurationSeconds(string html) => null;

    /// <summary>
    /// Error shown when no playable stream could be found on the page. VK
    /// overrides it with a more specific hint about private/region-locked
    /// videos.
    /// </summary>
    protected virtual string NoStreamError => Loc.Get(LocKeys.ErrScrapeNoStream);

    public virtual async Task<MediaPreview?> GetPreviewAsync(string url, CancellationToken ct = default)
    {
        var html = await FetchPageAsync(ResolvePageUrl(url), ct);
        var best = PickBest(ExtractStreams(html));

        return new MediaPreview
        {
            Title = ExtractTitle(html) ?? string.Empty,
            Author = ExtractAuthor(html) ?? string.Empty,
            Description = ExtractDescription(html) ?? string.Empty,
            ThumbnailUrl = ExtractThumbnail(html),
            Duration = ExtractDurationSeconds(html) is { } seconds ? TimeSpan.FromSeconds(seconds) : null,
            QualityText = best?.Quality,
            Platform = PlatformName
        };
    }

    public virtual async Task<IReadOnlyList<ResourceDetail>> GetDetailsAsync(string url, CancellationToken ct = default)
        => BuildDetails(await FetchPageAsync(ResolvePageUrl(url), ct));

    /// <summary>
    /// Metadata rows available from a scraped page. Subclasses override it to
    /// append platform-specific fields (e.g. VK view/like counts) without
    /// re-fetching the page.
    /// </summary>
    protected virtual IReadOnlyList<ResourceDetail> BuildDetails(string html)
    {
        var details = new List<ResourceDetail>();
        ResourceDetail.AddIfPresent(details, Loc.Get(LocKeys.DetailsTitle), ExtractTitle(html));
        ResourceDetail.AddIfPresent(details, Loc.Get(LocKeys.DetailsAuthor), ExtractAuthor(html));
        if (ExtractDurationSeconds(html) is { } seconds)
            ResourceDetail.AddIfPresent(details, Loc.Get(LocKeys.DetailsDuration), MediaPreview.FormatDuration(TimeSpan.FromSeconds(seconds)));
        ResourceDetail.AddIfPresent(details, Loc.Get(LocKeys.DetailsDescription), ExtractDescription(html));
        return details;
    }

    public virtual async Task<DownloadResult> DownloadAsync(
        string url, string outputPath,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        string? filePath = null;
        try
        {
            progress?.Report(new DownloadProgress(0, null, 0, Loc.Get(LocKeys.ProgressFetching)));

            var html = await FetchPageAsync(ResolvePageUrl(url), ct);
            var best = PickBest(ExtractStreams(html));
            if (best is null)
                return new DownloadResult(false, null, Loc.Get(LocKeys.ErrPlatformPrefix, PlatformName, NoStreamError));

            progress?.Report(new DownloadProgress(0, null, 0, Loc.Get(LocKeys.ProgressDownloading)));

            var title = ExtractTitle(html)?.Trim() ?? string.Empty;
            filePath = Path.Combine(outputPath, $"{SanitizeFileName(title.Length > 0 ? title : $"{PlatformName.ToLowerInvariant()}-video")}.mp4");

            using var request = new HttpRequestMessage(HttpMethod.Get, EnsureHttps(best.Value.Url));
            request.Headers.UserAgent.ParseAdd(UserAgent);
            if (Uri.TryCreate(ResolvePageUrl(url), UriKind.Absolute, out var referer))
                request.Headers.Referrer = referer;
            ApplyDownloadHeaders(request, url);

            using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            await using var fileStream = File.Create(filePath);

            var buffer = new byte[81920];
            long totalRead = 0;
            int read;

            while ((read = await stream.ReadAsync(buffer, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
                totalRead += read;
                progress?.Report(new DownloadProgress(totalRead, totalBytes,
                    totalBytes > 0 ? (double)totalRead / totalBytes : null, Loc.Get(LocKeys.ProgressDownloading)));
            }

            return new DownloadResult(true, filePath, null);
        }
        catch (OperationCanceledException)
        {
            TryDeleteFile(filePath);
            return new DownloadResult(false, null, Loc.Get(LocKeys.ErrCancelled));
        }
        catch (Exception ex)
        {
            // A network error mid-download leaves a partial file behind; clean it up.
            TryDeleteFile(filePath);
            return new DownloadResult(false, null, Loc.Get(LocKeys.ErrPlatformPrefix, PlatformName, ex.Message));
        }
    }

    protected virtual async Task<string> FetchPageAsync(string url, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, EnsureHttps(url));
        request.Headers.UserAgent.ParseAdd(UserAgent);
        using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    /// <summary>
    /// Adds downloader-specific headers to the media request (e.g. Origin for
    /// CDNs that reject requests from unknown referrers).
    /// </summary>
    protected virtual void ApplyDownloadHeaders(HttpRequestMessage request, string url) { }

    /// <summary>
    /// Upgrades a plain-http URL to https. Page and media requests never go
    /// out over an unencrypted connection.
    /// </summary>
    private static string EnsureHttps(string url) =>
        url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            ? "https://" + url[7..]
            : url;

    protected static (string Quality, string Url)? PickBest(IEnumerable<(string Quality, string Url)> streams)
    {
        (string Quality, string Url)? best = null;
        var bestQuality = -1;

        foreach (var (quality, streamUrl) in streams)
        {
            if (string.IsNullOrWhiteSpace(streamUrl))
                continue;

            var parsed = ParseQuality(quality);
            if (parsed > bestQuality)
            {
                bestQuality = parsed;
                best = (quality, streamUrl);
            }
        }

        return best;
    }

    protected static int ParseQuality(string quality)
    {
        var match = Regex.Match(quality, @"\d+");
        return match.Success && int.TryParse(match.Value, out var value) ? value : 0;
    }

    protected static string? GetMetaProperty(string html, string property)
    {
        var pattern = $@"<meta[^>]+property=[""']{Regex.Escape(property)}[""'][^>]+content=[""'](?<value>[^""']*)[""']";

        var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            match = Regex.Match(html,
                $@"<meta[^>]+content=[""'](?<value>[^""']*)[""'][^>]+property=[""']{Regex.Escape(property)}[""']",
                RegexOptions.IgnoreCase);
        }

        return match.Success ? DecodeEscapedText(match.Groups["value"].Value) : null;
    }

    /// <summary>
    /// Cleans meta content: HTML entities plus any JSON \uXXXX escapes some
    /// platforms (e.g. VK) embed instead of real characters.
    /// </summary>
    private static string DecodeEscapedText(string value)
    {
        var decoded = Regex.Replace(value, @"\\u([0-9a-fA-F]{4})",
            match => ((char)Convert.ToInt32(match.Groups[1].Value, 16)).ToString());
        return WebUtility.HtmlDecode(decoded);
    }

    private static void TryDeleteFile(string? path)
    {
        try
        {
            if (path is not null)
                File.Delete(path);
        }
        catch (IOException)
        {
        }
    }

    private static string SanitizeFileName(string name)
    {
        // Strip characters invalid on Windows and macOS, plus control
        // characters, then trim and clamp the length so the file name stays
        // valid on both platforms.
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var builder = new System.Text.StringBuilder(name.Length);
        foreach (var c in name)
            builder.Append(invalid.Contains(c) || char.IsControl(c) ? '_' : c);

        const int maxLength = 120;
        var sanitized = builder.ToString().Trim().TrimEnd('.', ' ');
        return sanitized.Length <= maxLength
            ? sanitized
            : sanitized[..maxLength].TrimEnd('.', ' ');
    }
}
