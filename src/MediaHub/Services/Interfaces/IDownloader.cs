namespace MediaHub.Services.Interfaces;

public interface IDownloader
{
    string PlatformName { get; }
    bool CanHandle(string url);
    Task<DownloadResult> DownloadAsync(string url, string outputPath, IProgress<DownloadProgress>? progress = null, CancellationToken ct = default);
}

public readonly record struct DownloadResult(bool Success, string? FilePath, string? ErrorMessage);
public readonly record struct DownloadProgress(long BytesReceived, long? TotalBytes, double? Percentage, string Status);
