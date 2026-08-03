using System.Net.Http;
using System.Text;
using MediaHub.Models;
using MediaHub.Services.Interfaces;
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
        for (var attempt = 0; ; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var publication = await _tikTok.Publications.GetAsync(url, ct);

                // Video posts have no cover of their own in the public model, so fall
                // back to the soundtrack artwork, then the author avatar, and finally
                // the first photo for image posts.
                string? thumbnail = null;
                if (publication.Video is not null)
                {
                    thumbnail = publication.Soundtrack?.LargeCoverUrl
                        ?? publication.Soundtrack?.MediumCoverUrl
                        ?? publication.Soundtrack?.ThumbCoverUrl
                        ?? publication.Author?.MediumAvatarUrl;
                }
                else if (publication.Images is { Count: > 0 })
                {
                    thumbnail = publication.Images[0].Url;
                }

                var description = publication.Description?.Trim();
                if (description is { Length: > 100 })
                    description = description[..100] + "...";

                var quality = publication.Video is not null
                    ? Loc.Get(LocKeys.QualityVideo)
                    : publication.Images is { Count: > 0 } images ? Loc.Get(LocKeys.QualityImages, images.Count) : null;

                return new MediaPreview
                {
                    Title = description ?? string.Empty,
                    Author = publication.Author?.Nickname ?? string.Empty,
                    ThumbnailUrl = thumbnail,
                    Duration = publication.Video is { Duration: > 0 } video
                        ? TimeSpan.FromMilliseconds(video.Duration)
                        : null,
                    QualityText = quality,
                    Platform = PlatformName
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
        var publication = await _tikTok.Publications.GetAsync(url, ct);
        var details = new List<ResourceDetail>();
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

            var publication = await _tikTok.Publications.GetAsync(url, ct);
            var baseName = SanitizeFileName($"{publication.Author.Nickname}_{publication.Id}");

            if (publication.Video is not null)
            {
                progress?.Report(new DownloadProgress(0, null, 0, Loc.Get(LocKeys.ProgressDownloadingVideo)));

                var downloadProgress = new Progress<double>(p =>
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

                var downloadProgress = new Progress<double>(p =>
                    progress?.Report(new DownloadProgress(0, null, p, Loc.Get(LocKeys.ProgressDownloadingImages))));

                var dirPath = Path.Combine(outputPath, baseName);
                filePath = dirPath;
                await _tikTok.DownloadImagesAsync(publication.Images, dirPath, baseName, downloadProgress, ct);

                return new DownloadResult(true, dirPath, null);
            }

            return new DownloadResult(false, null, Loc.Get(LocKeys.ErrNoContent));
        }
        catch (OperationCanceledException)
        {
            TryDeleteFile(filePath);
            return new DownloadResult(false, null, Loc.Get(LocKeys.ErrCancelled));
        }
        catch (TikTokExplodeException ex)
        {
            // Clean, library-level error (invalid link, private publication, API failure).
            TryDeleteFile(filePath);
            return new DownloadResult(false, null, ex.Message);
        }
        catch (Exception ex)
        {
            // A network or disk error mid-download leaves a partial file or
            // image directory behind; clean it up.
            TryDeleteFile(filePath);
            return new DownloadResult(false, null, Loc.Get(LocKeys.ErrPlatformPrefix, PlatformName, ex.Message));
        }
    }

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
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();

        var builder = new StringBuilder(fileName.Length);
        foreach (var c in fileName)
            builder.Append(invalid.Contains(c) || char.IsControl(c) ? '_' : c);

        const int maxLength = 120;
        var sanitized = builder.ToString().Trim().TrimEnd('.', ' ');
        return sanitized.Length <= maxLength
            ? sanitized
            : sanitized[..maxLength].TrimEnd('.', ' ');
    }
}
