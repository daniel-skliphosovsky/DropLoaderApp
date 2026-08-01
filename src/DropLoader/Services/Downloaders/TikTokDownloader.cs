using System.Text;
using DropLoader.Services.Interfaces;
using TikTokExplode;
using TikTokExplode.Exceptions;

namespace DropLoader.Services.Downloaders;

public sealed class TikTokDownloader : IDownloader
{
    public string PlatformName => "TikTok";

    private readonly TikTokClient _tikTok;

    public TikTokDownloader(TikTokClient tikTok)
    {
        _tikTok = tikTok;
    }

    public bool CanHandle(string url) =>
        UrlHelpers.UrlBelongsTo(url, "tiktok.com", "vm.tiktok.com");

    public async Task<DownloadResult> DownloadAsync(
        string url, string outputPath,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        string? filePath = null;
        try
        {
            progress?.Report(new DownloadProgress(0, null, 0, "Fetching metadata..."));

            var publication = await _tikTok.Publications.GetAsync(url, ct);
            var baseName = SanitizeFileName($"{publication.Author.Nickname}_{publication.Id}");

            if (publication.Video is not null)
            {
                progress?.Report(new DownloadProgress(0, null, 0, "Downloading video..."));

                var downloadProgress = new Progress<double>(p =>
                    progress?.Report(new DownloadProgress(0, null, p, "Downloading video...")));

                // The library appends the .mp4 extension itself, so the
                // custom file name is passed without it.
                filePath = Path.Combine(outputPath, $"{baseName}.mp4");
                await _tikTok.DownloadVideoAsync(publication.Video, outputPath, baseName, downloadProgress, ct);

                return new DownloadResult(true, filePath, null);
            }

            if (publication.Images is { Count: > 0 })
            {
                progress?.Report(new DownloadProgress(0, null, 0, "Downloading images..."));

                var downloadProgress = new Progress<double>(p =>
                    progress?.Report(new DownloadProgress(0, null, p, "Downloading images...")));

                var dirPath = Path.Combine(outputPath, baseName);
                await _tikTok.DownloadImagesAsync(publication.Images, dirPath, baseName, downloadProgress, ct);

                return new DownloadResult(true, dirPath, null);
            }

            return new DownloadResult(false, null, "No downloadable content found");
        }
        catch (OperationCanceledException)
        {
            TryDeleteFile(filePath);
            return new DownloadResult(false, null, "Cancelled");
        }
        catch (TikTokExplodeException ex)
        {
            // Clean, library-level error (invalid link, private publication, API failure).
            return new DownloadResult(false, null, ex.Message);
        }
        catch (Exception ex)
        {
            return new DownloadResult(false, null, $"TikTok: {ex.Message}");
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
