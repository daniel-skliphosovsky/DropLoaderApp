using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using MediaHub.Models;
using MediaHub.Services.Interfaces;
using MediaHub.Services.Logging;
using TikTokExplode;
using TikTokExplode.Exceptions;

namespace MediaHub.Services.Downloaders;

public sealed class TikTokDownloader : IDownloader
{
    public string PlatformName => "TikTok";

    // Preview hits the API once; network blips that escape the library's
    // own retry loop are retried a few times with a short delay so the
    // preview succeeds on the second try. Invalid links fail fast.
    private const int PreviewMaxAttempts = 4;
    private const int PreviewRetryDelayMs = 400;

    private readonly TikTokClient _tikTok;

    public TikTokDownloader(TikTokClient tikTok)
    {
        _tikTok = tikTok;
    }

    public bool CanHandle(string url) =>
        UrlHelpers.UrlBelongsTo(url,
            "tiktok.com", "vm.tiktok.com", "vt.tiktok.com", "www.tiktok.com", "m.tiktok.com");

    public async Task<MediaPreview?> GetPreviewAsync(string url, CancellationToken ct = default)
    {
        for (int attempt = 0; ; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                TikTokExplode.Publications.Publication publication = await _tikTok.Publications.GetAsync(url, ct);

                string? description = publication.Description?.Trim();
                if (description is { Length: > 100 })
                    description = description[..100] + "...";

                string? quality = publication.Video is not null
                    ? Loc.Get(LocKeys.QualityVideo)
                    : publication.Images is { Count: > 0 } images ? Loc.Get(LocKeys.QualityImages, images.Count) : null;

                return new MediaPreview
                {
                    Title = description ?? string.Empty,
                    Author = publication.Author?.Nickname ?? string.Empty,
                    Duration = publication.Video is { Duration: > 0 } video
                        ? TimeSpan.FromMilliseconds(video.Duration)
                        : null,
                    QualityText = quality
                };
            }
            catch (Exception ex) when (attempt < PreviewMaxAttempts - 1 && IsTransient(ex, ct))
            {
                await Task.Delay(PreviewRetryDelayMs, ct);
            }
        }
    }

    /// <summary>
    /// True for network-level failures that surface as HttpRequestException
    /// or a timeout. The library already retries rate limiting and 5xx
    /// internally, so retrying them here too would duplicate the budget.
    /// Invalid links, private publications and API 4xx fail fast.
    /// </summary>
    private static bool IsTransient(Exception ex, CancellationToken ct)
    {
        if (ex is HttpRequestException)
            return true;
        if (ex is TikTokExplodeException tikTok)
            return tikTok.InnerException is HttpRequestException;
        // A timeout cancels the library's internal request without the
        // caller's token; a real cancellation must propagate immediately.
        return ex is OperationCanceledException && !ct.IsCancellationRequested;
    }

    public async Task<IReadOnlyList<ResourceDetail>> GetDetailsAsync(string url, CancellationToken ct = default)
    {
        TikTokExplode.Publications.Publication publication = await _tikTok.Publications.GetAsync(url, ct);
        List<ResourceDetail> details = new List<ResourceDetail>();
        ResourceDetail.AddIfPresent(details, Loc.Get(LocKeys.DetailsDescription), publication.Description);
        ResourceDetail.AddIfPresent(details, Loc.Get(LocKeys.DetailsAuthor), publication.Author?.Nickname);
        ResourceDetail.AddIfPresent(details, Loc.Get(LocKeys.DetailsAuthorId), publication.Author?.UserId);
        ResourceDetail.AddIfPresent(details, Loc.Get(LocKeys.DetailsRegion), publication.Author?.Region);
        if (publication.Author is { IsVerified: true })
            details.Add(new ResourceDetail(Loc.Get(LocKeys.DetailsVerified), Loc.Get(LocKeys.DetailsYes)));

        if (publication.Video is { Duration: > 0 } video)
            ResourceDetail.AddIfPresent(details, Loc.Get(LocKeys.DetailsDuration),
                MediaPreview.FormatDuration(TimeSpan.FromMilliseconds(video.Duration)));

        if (publication.Statistics is { } stats)
        {
            AddCount(details, Loc.Get(LocKeys.DetailsLikes), stats.DiggCount);
            AddCount(details, Loc.Get(LocKeys.DetailsViews), stats.PlayCount);
            AddCount(details, Loc.Get(LocKeys.DetailsComments), stats.CommentCount);
            AddCount(details, Loc.Get(LocKeys.DetailsShares), stats.ShareCount);
            AddCount(details, Loc.Get(LocKeys.DetailsDownloads), stats.DownloadCount);
        }

        if (publication.Soundtrack is { } soundtrack &&
            !string.IsNullOrWhiteSpace(soundtrack.Title) &&
            !string.IsNullOrWhiteSpace(soundtrack.Author))
        {
            details.Add(new ResourceDetail(Loc.Get(LocKeys.DetailsSound), $"{soundtrack.Title} - {soundtrack.Author}"));
        }

        return details;
    }

    private static void AddCount(List<ResourceDetail> details, string label, ulong count)
    {
        if (count > 0)
            details.Add(new ResourceDetail(label, count.ToString("N0")));
    }

    public async Task<DownloadResult> DownloadAsync(
        string url, string outputPath,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        string? filePath = null;
        try
        {
            progress?.Report(new DownloadProgress(0, null, 0, Loc.Get(LocKeys.ProgressFetchingMetadata)));

            TikTokExplode.Publications.Publication publication = await _tikTok.Publications.GetAsync(url, ct);
            string title = publication.Description?.Trim() ?? string.Empty;
            string author = publication.Author?.Nickname ?? string.Empty;
            string baseName = SanitizeFileName(
                !string.IsNullOrWhiteSpace(title)
                    ? (string.IsNullOrWhiteSpace(author) ? title : $"{title} - {author}")
                    : publication.Id);
            if (string.IsNullOrWhiteSpace(baseName))
                baseName = publication.Id;

            if (publication.Video is not null)
            {
                progress?.Report(new DownloadProgress(0, null, 0, Loc.Get(LocKeys.ProgressDownloadingVideo)));

                Progress<double> downloadProgress = new Progress<double>(p =>
                    progress?.Report(new DownloadProgress(0, null, p, Loc.Get(LocKeys.ProgressDownloadingVideo))));

                // The library appends the .mp4 extension itself, so the
                // custom file name is passed without it.
                filePath = Path.Combine(outputPath, $"{baseName}.mp4");
                await _tikTok.DownloadVideoAsync(publication.Video, outputPath, baseName, downloadProgress, ct);

                return new DownloadResult(true, filePath, null);
            }

            if (publication.Images is { Count: > 0 })
            {
                progress?.Report(new DownloadProgress(0, null, 0, Loc.Get(LocKeys.ProgressDownloadingImages)));

                Progress<double> downloadProgress = new Progress<double>(p =>
                    progress?.Report(new DownloadProgress(0, null, p, Loc.Get(LocKeys.ProgressDownloadingImages))));

                string dirPath = Path.Combine(outputPath, baseName);
                filePath = dirPath;
                await _tikTok.DownloadImagesAsync(publication.Images, dirPath, baseName, downloadProgress, ct);

                return new DownloadResult(true, dirPath, null);
            }

            return new DownloadResult(false, null, Loc.Get(LocKeys.ErrNoContent));
        }
        catch (OperationCanceledException ex)
        {
            TryDeleteFile(filePath);
            if (ct.IsCancellationRequested)
                return new DownloadResult(false, null, Loc.Get(LocKeys.ErrCancelled));

            // A timeout surfaces as TaskCanceledException, not a user
            // cancellation: report it as a network problem.
            AppLogger.Log(ex);
            return new DownloadResult(false, null, Loc.Get(LocKeys.ErrNetwork));
        }
        catch (TikTokExplodeException ex)
        {
            // Library-level error (invalid link, private publication, API
            // failure); network-level failures inside it map to the friendly
            // network message instead of the raw library text.
            TryDeleteFile(filePath);
            AppLogger.Log(ex);
            return IsNetworkError(ex)
                ? new DownloadResult(false, null, Loc.Get(LocKeys.ErrNetwork))
                : new DownloadResult(false, null, Loc.Get(LocKeys.ErrPlatformUnavailable, PlatformName));
        }
        catch (Exception ex) when (IsNetworkError(ex))
        {
            // A network error mid-download leaves a partial file or image
            // directory behind; clean it up.
            TryDeleteFile(filePath);
            AppLogger.Log(ex);
            return new DownloadResult(false, null, Loc.Get(LocKeys.ErrNetwork));
        }
        catch (Exception ex)
        {
            // A disk error or any other unexpected failure mid-download leaves
            // a partial file or image directory behind; clean it up.
            TryDeleteFile(filePath);
            AppLogger.Log(ex);
            return new DownloadResult(false, null, Loc.Get(LocKeys.ErrPlatformUnavailable, PlatformName));
        }
    }

    private static bool IsNetworkError(Exception ex) =>
        ex is HttpRequestException or WebException or SocketException ||
        ex.InnerException is HttpRequestException or WebException or SocketException;

    private static void TryDeleteFile(string? path)
    {
        try
        {
            if (path is null)
                return;

            if (Directory.Exists(path))
                Directory.Delete(path, true);
            else
                File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        // Strip characters invalid on Windows and macOS, plus control
        // characters, then trim and clamp the length so the file name
        // stays valid everywhere.
        HashSet<char> invalid = Path.GetInvalidFileNameChars().ToHashSet();

        StringBuilder builder = new StringBuilder(fileName.Length);
        foreach (var c in fileName)
            builder.Append(invalid.Contains(c) || char.IsControl(c) ? '_' : c);

        const int maxLength = 120;
        string sanitized = builder.ToString().Trim().TrimEnd('.', ' ');
        return sanitized.Length <= maxLength
            ? sanitized
            : sanitized[..maxLength].TrimEnd('.', ' ');
    }
}
