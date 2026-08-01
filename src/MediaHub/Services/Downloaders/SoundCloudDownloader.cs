using System.Net;
using MediaHub.Models;
using MediaHub.Services.Interfaces;
using SoundCloudExplode;
using SoundCloudExplode.Tracks;

namespace MediaHub.Services.Downloaders;

public sealed class SoundCloudDownloader : IDownloader
{
    public string PlatformName => "SoundCloud";

    // SoundCloudExplode 1.6.7 ships a dead client id (wDSKS1Bp...) that the
    // API rejects with 401. The live page embeds a fresh client id, so we
    // scrape it once and cache it, falling back to a known-good session id
    // if the page stops exposing one.
    private const string FallbackClientId = "yNSW5UvBmb1A5j7qPUtIMuB9Itx3jsOC";

    private static readonly SemaphoreSlim ClientIdLock = new(1, 1);
    private static string? _clientId;

    private readonly HttpClient _http;

    public SoundCloudDownloader(HttpClient httpClient) => _http = httpClient;

    public bool CanHandle(string url) =>
        UrlHelpers.UrlBelongsTo(url, "soundcloud.com", "on.soundcloud.com");

    public async Task<MediaPreview?> GetPreviewAsync(string url, CancellationToken ct = default)
    {
        var track = await GetTrackAsync(url, ct);
        if (track is null)
            return null;

        return new MediaPreview
        {
            Title = track.Title ?? string.Empty,
            Author = track.User?.Username ?? string.Empty,
            Description = track.Description ?? string.Empty,
            ThumbnailUrl = track.ArtworkUrl?.ToString(),
            Duration = track.Duration is { } milliseconds ? TimeSpan.FromMilliseconds(milliseconds) : null,
            QualityText = Loc.Get(LocKeys.QualityAudio),
            Platform = PlatformName
        };
    }

    public async Task<IReadOnlyList<ResourceDetail>> GetDetailsAsync(string url, CancellationToken ct = default)
    {
        var track = await GetTrackAsync(url, ct);
        if (track is null)
            return [];

        var details = new List<ResourceDetail>();
        ResourceDetail.AddIfPresent(details, Loc.Get(LocKeys.DetailsTitle), track.Title);
        ResourceDetail.AddIfPresent(details, Loc.Get(LocKeys.DetailsAuthor), track.User?.Username);
        if (track.Duration is { } milliseconds)
            ResourceDetail.AddIfPresent(details, Loc.Get(LocKeys.DetailsDuration),
                MediaPreview.FormatDuration(TimeSpan.FromMilliseconds(milliseconds)));
        ResourceDetail.AddIfPresent(details, Loc.Get(LocKeys.DetailsGenre), track.Genre);
        ResourceDetail.AddIfPresent(details, Loc.Get(LocKeys.DetailsPlays), track.PlaybackCount?.ToString("N0"));
        ResourceDetail.AddIfPresent(details, Loc.Get(LocKeys.DetailsLikes), track.LikesCount?.ToString("N0"));
        ResourceDetail.AddIfPresent(details, Loc.Get(LocKeys.DetailsComments), track.CommentCount?.ToString("N0"));
        ResourceDetail.AddIfPresent(details, Loc.Get(LocKeys.DetailsDownloads), track.DownloadCount?.ToString("N0"));
        ResourceDetail.AddIfPresent(details, Loc.Get(LocKeys.DetailsDownloadable), track.Downloadable.ToString());
        ResourceDetail.AddIfPresent(details, Loc.Get(LocKeys.DetailsPosted), track.CreatedAt.ToString("d"));
        ResourceDetail.AddIfPresent(details, Loc.Get(LocKeys.DetailsDescription), track.Description);
        ResourceDetail.AddIfPresent(details, Loc.Get(LocKeys.DetailsLink), track.PermalinkUrl?.ToString());
        return details;
    }

    public async Task<DownloadResult> DownloadAsync(
        string url, string outputPath,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        string? filePath = null;
        try
        {
            progress?.Report(new DownloadProgress(0, null, 0, Loc.Get(LocKeys.ProgressFetching)));

            var track = await GetTrackAsync(url, ct);
            if (track == null)
                return new DownloadResult(false, null, Loc.Get(LocKeys.ErrTrackNotFound));

            var streamUrl = await GetDownloadUrlAsync(track, ct);
            if (string.IsNullOrEmpty(streamUrl))
                return new DownloadResult(false, null, Loc.Get(LocKeys.ErrNoStreamUrl));

            filePath = Path.Combine(outputPath, $"{SanitizeFileName(track.Title ?? "track")}.mp3");

            using var response = await _http.GetAsync(streamUrl, HttpCompletionOption.ResponseHeadersRead, ct);
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
            return new DownloadResult(false, null, Loc.Get(LocKeys.ErrPlatformPrefix, PlatformName, ex.Message));
        }
    }

    /// <summary>
    /// Resolves a track with the cached client id. A 401 means the cached id
    /// was revoked, so it is dropped and the request retried once with a
    /// freshly scraped id.
    /// </summary>
    private async Task<Track?> GetTrackAsync(string url, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var client = new SoundCloudClient(await ResolveClientIdAsync(ct), _http);
            try
            {
                return await client.Tracks.GetAsync(url, ct);
            }
            catch (HttpRequestException ex) when (attempt == 0 && ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                _clientId = null;
            }
        }

        return null;
    }

    private async Task<string?> GetDownloadUrlAsync(Track track, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var client = new SoundCloudClient(await ResolveClientIdAsync(ct), _http);
            try
            {
                return await client.Tracks.GetDownloadUrlAsync(track, ct);
            }
            catch (HttpRequestException ex) when (attempt == 0 && ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                _clientId = null;
            }
        }

        return null;
    }

    private async Task<string> ResolveClientIdAsync(CancellationToken ct)
    {
        if (_clientId is not null)
            return _clientId;

        await ClientIdLock.WaitAsync(ct);
        try
        {
            if (_clientId is null)
                _clientId = await FetchClientIdAsync(ct) ?? FallbackClientId;
        }
        finally
        {
            ClientIdLock.Release();
        }

        return _clientId;
    }

    private async Task<string?> FetchClientIdAsync(CancellationToken ct)
    {
        try
        {
            // Scrape the client id the same way SoundCloudExplode does, via a
            // throwaway client so any failure stays local and we can fall back.
            var probe = new SoundCloudClient(_http);
            var id = await probe.GetClientIdAsync(ct);
            return string.IsNullOrWhiteSpace(id) ? null : id;
        }
        catch
        {
            return null;
        }
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
