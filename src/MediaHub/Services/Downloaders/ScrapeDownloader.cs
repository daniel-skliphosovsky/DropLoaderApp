using System.Net;
using System.Text.RegularExpressions;
using MediaHub.Models;
using MediaHub.Services.Interfaces;

namespace MediaHub.Services.Downloaders;

/// <summary>
/// Base for downloaders that scrape an HTML page for a direct mp4 link
/// without any API key or login (VK, Vimeo). Metadata comes from the page
/// itself (og tags or embedded JSON), the file is streamed like everywhere
/// else in the app.
/// </summary>
public abstract class ScrapeDownloader : IDownloader
{
    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";

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
    protected virtual long? ExtractDurationSeconds(string html) => null;

    public virtual async Task<MediaPreview?> GetPreviewAsync(string url, CancellationToken ct = default)
    {
        var html = await FetchPageAsync(ResolvePageUrl(url), ct);
        var best = PickBest(ExtractStreams(html));

        return new MediaPreview
        {
            Title = ExtractTitle(html) ?? string.Empty,
            Author = ExtractAuthor(html) ?? string.Empty,
            ThumbnailUrl = ExtractThumbnail(html),
            Duration = ExtractDurationSeconds(html) is { } seconds ? TimeSpan.FromSeconds(seconds) : null,
            QualityText = best?.Quality,
            Platform = PlatformName
        };
    }

    public virtual async Task<DownloadResult> DownloadAsync(
        string url, string outputPath,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        string? filePath = null;
        try
        {
            progress?.Report(new DownloadProgress(0, null, 0, "Fetching..."));

            var html = await FetchPageAsync(ResolvePageUrl(url), ct);
            var best = PickBest(ExtractStreams(html));
            if (best is null)
                return new DownloadResult(false, null,
                    $"{PlatformName}: could not extract a video link (the page may require login or be region-restricted)");

            progress?.Report(new DownloadProgress(0, null, 0, "Downloading..."));

            var title = ExtractTitle(html)?.Trim() ?? string.Empty;
            filePath = Path.Combine(outputPath, $"{SanitizeFileName(title.Length > 0 ? title : $"{PlatformName.ToLowerInvariant()}-video")}.mp4");

            using var request = new HttpRequestMessage(HttpMethod.Get, best.Value.Url);
            request.Headers.UserAgent.ParseAdd(UserAgent);
            if (Uri.TryCreate(ResolvePageUrl(url), UriKind.Absolute, out var referer))
                request.Headers.Referrer = referer;

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
                    totalBytes > 0 ? (double)totalRead / totalBytes : null, "Downloading..."));
            }

            return new DownloadResult(true, filePath, null);
        }
        catch (OperationCanceledException)
        {
            TryDeleteFile(filePath);
            return new DownloadResult(false, null, "Cancelled");
        }
        catch (Exception ex)
        {
            return new DownloadResult(false, null, $"{PlatformName}: {ex.Message}");
        }
    }

    protected async Task<string> FetchPageAsync(string url, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd(UserAgent);
        using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

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

        return match.Success ? WebUtility.HtmlDecode(match.Groups["value"].Value) : null;
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

    private static string SanitizeFileName(string name) =>
        string.Join("_", name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries))
            .Trim().TrimEnd('.', ' ');
}
