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

    /// <summary>
    /// True when the URL points to a playlist/set of multiple items instead
    /// of a single piece of media. Defaults to false; only platforms that
    /// support playlists override it.
    /// </summary>
    bool IsPlaylistUrl(string url) => false;

    /// <summary>
    /// The playable items of the playlist at the given URL, in play order.
    /// Each item carries its own download URL and title. Returns an empty
    /// list for single items or when the playlist cannot be resolved.
    /// </summary>
    Task<IReadOnlyList<PlaylistItem>> GetPlaylistItemsAsync(string url, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<PlaylistItem>>([]);

    /// <summary>
    /// The display name of the playlist/set at the given URL, used as the
    /// name of the subfolder the items are saved into. Returns null for
    /// single items or when the title cannot be resolved.
    /// </summary>
    Task<string?> GetPlaylistTitleAsync(string url, CancellationToken ct = default)
        => Task.FromResult<string?>(null);

    /// <summary>
    /// Full metadata rows for the resource (author, stats, dates, description),
    /// shown in the preview card's "Information" section. Defaults to nothing
    /// extra; platforms that expose more metadata override it.
    /// </summary>
    Task<IReadOnlyList<ResourceDetail>> GetDetailsAsync(string url, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<ResourceDetail>>([]);
}

public readonly record struct DownloadResult(bool Success, string? FilePath, string? ErrorMessage);
public readonly record struct DownloadProgress(long BytesReceived, long? TotalBytes, double? Percentage, string Status);
public readonly record struct PlaylistItem(string Url, string Title);
