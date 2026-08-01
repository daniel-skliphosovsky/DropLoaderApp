using MediaHub.Models;

namespace MediaHub.Services.Interfaces;

public interface IDownloader
{
    string PlatformName { get; }
    bool CanHandle(string url);
    Task<DownloadResult> DownloadAsync(string url, string outputPath, IProgress<DownloadProgress>? progress = null, CancellationToken ct = default);

    /// <summary>
    /// Resolves lightweight metadata (title, author, thumbnail, quality) for
    /// the preview card. Never downloads anything; may return null when the
    /// metadata cannot be resolved.
    /// </summary>
    Task<MediaPreview?> GetPreviewAsync(string url, CancellationToken ct = default);
}

public readonly record struct DownloadResult(bool Success, string? FilePath, string? ErrorMessage);
public readonly record struct DownloadProgress(long BytesReceived, long? TotalBytes, double? Percentage, string Status);
