using DropLoaderApp.Services.Interfaces;
using TikTokExplode;
using TikTokExplode.Domain.Entities;
using TikTokExplode.Domain.Enums;

namespace DropLoaderApp.Services.Downloaders;

public sealed class TikTokDownloader : IDownloader
{
    public string PlatformName => "TikTok";

    private readonly ITikTokClient _tikTok;

    public TikTokDownloader(ITikTokClient tikTok)
    {
        _tikTok = tikTok;
    }

    public bool CanHandle(string url) =>
        !string.IsNullOrWhiteSpace(url) &&
        (url.Contains("tiktok.com") || url.Contains("tiktok"));

    public async Task<DownloadResult> DownloadAsync(
        string url, string outputPath,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        try
        {
            Publication publication;

            try
            {
                progress?.Report(new DownloadProgress(0, null, 0, "Fetching metadata..."));
                publication = await _tikTok.GetPublicationAsync(url, ct);
            }
            catch (Exception ex)
            {
                return new DownloadResult(false, null, $"Failed to get TikTok data: {ex.Message}");
            }

            if (publication.Type == PublicationType.Video && publication.Video is not null)
            {
                var videoUrl = publication.Video.PlayUrl;
                var fileName = SanitizeFileName($"{publication.Author.Nickname}_{publication.Id}.mp4");
                var filePath = Path.Combine(outputPath, fileName);

                progress?.Report(new DownloadProgress(0, null, 0, "Downloading video..."));

                var fileProgress = new Progress<long>(bytes =>
                    progress?.Report(new DownloadProgress(bytes, null, null, "Downloading video...")));

                await _tikTok.DownloadVideoAsync(videoUrl, filePath, fileProgress, ct);

                return new DownloadResult(true, filePath, null);
            }
            else if (publication.Images is { Count: > 0 })
            {
                var dirName = SanitizeFileName($"{publication.Author.Nickname}_{publication.Id}");
                var dirPath = Path.Combine(outputPath, dirName);
                Directory.CreateDirectory(dirPath);

                progress?.Report(new DownloadProgress(0, publication.Images.Count, 0, "Downloading images..."));

                var imageProgress = new Progress<long>(downloaded =>
                    progress?.Report(new DownloadProgress(downloaded, publication.Images.Count,
                        (double)downloaded / publication.Images.Count, "Downloading images...")));

                await _tikTok.DownloadImagesAsync(publication.Images, dirPath, imageProgress, ct);

                return new DownloadResult(true, dirPath, null);
            }

            return new DownloadResult(false, null, "No downloadable content found");
        }
        catch (OperationCanceledException)
        {
            return new DownloadResult(false, null, "Cancelled");
        }
        catch (Exception ex)
        {
            return new DownloadResult(false, null, $"TikTok: {ex.Message}");
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
    }
}
