using MediaHub.Models;
using MediaHub.Services.Interfaces;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;

namespace MediaHub.Services.Downloaders;

public sealed class YouTubeDownloader : IDownloader
{
    public string PlatformName => "YouTube";

    private readonly YoutubeClient _client;

    public YouTubeDownloader(HttpClient httpClient)
    {
        _client = new YoutubeClient(httpClient);
    }

    public bool CanHandle(string url) =>
        UrlHelpers.UrlBelongsTo(url, "youtube.com", "youtu.be");

    /// <summary>
    /// A YouTube URL is a playlist when it carries a "list=" query parameter
    /// (playlists, mixes, watch-with-list, youtu.be short links).
    /// </summary>
    public bool IsPlaylistUrl(string url) => TryGetPlaylistId(url, out _);

    /// <summary>
    /// Resolves the playlist id from the URL and enumerates its videos in
    /// play order, each as its own watch URL so the shared single-video
    /// download path can be reused for every item.
    /// </summary>
    public async Task<IReadOnlyList<PlaylistItem>> GetPlaylistItemsAsync(string url, CancellationToken ct = default)
    {
        if (!TryGetPlaylistId(url, out var playlistId) || playlistId is null)
            return [];

        var items = new List<PlaylistItem>();
        await foreach (var video in _client.Playlists.GetVideosAsync(playlistId, ct))
            items.Add(new PlaylistItem($"https://www.youtube.com/watch?v={video.Id.Value}", video.Title));

        return items;
    }

    private static bool TryGetPlaylistId(string url, out string? playlistId)
    {
        playlistId = null;

        var value = string.IsNullOrWhiteSpace(url) ? string.Empty : url.Trim();
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            !Uri.TryCreate("https://" + value, UriKind.Absolute, out uri))
            return false;

        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split('=', 2);
            if (kv.Length == 2 && kv[0].Equals("list", StringComparison.OrdinalIgnoreCase) && kv[1].Length > 0)
            {
                playlistId = kv[1];
                return true;
            }
        }

        return false;
    }

    public async Task<MediaPreview?> GetPreviewAsync(string url, CancellationToken ct = default)
    {
        var video = await _client.Videos.GetAsync(url, ct);

        var thumbnail = video.Thumbnails.TryGetWithHighestResolution();
        string? quality = null;

        try
        {
            // Stream resolution is a nice-to-have; if it fails we still have
            // the rest of the metadata, so it must not break the preview.
            var manifest = await _client.Videos.Streams.GetManifestAsync(video.Id, ct);
            var best = manifest.GetMuxedStreams()
                .Where(s => s.Container == Container.Mp4)
                .MaxBy(s => s.VideoQuality);

            quality = best is not null
                ? $"{best.VideoQuality.Label} MP4"
                : manifest.GetAudioOnlyStreams().Any() ? "Audio" : null;
        }
        catch (Exception)
        {
        }

        return new MediaPreview
        {
            Title = video.Title,
            Author = video.Author.ChannelTitle,
            ThumbnailUrl = thumbnail?.Url,
            Duration = video.Duration,
            QualityText = quality,
            Platform = PlatformName
        };
    }

    public async Task<DownloadResult> DownloadAsync(
        string url, string outputPath,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        try
        {
            progress?.Report(new DownloadProgress(0, null, 0, "Fetching..."));

            var video = await _client.Videos.GetAsync(url, ct);
            var manifest = await _client.Videos.Streams.GetManifestAsync(video.Id, ct);

            var streamInfo = manifest.GetMuxedStreams()
                .Where(s => s.Container == Container.Mp4)
                .MaxBy(s => s.VideoQuality) as IStreamInfo
                ?? manifest.GetAudioOnlyStreams().FirstOrDefault();

            if (streamInfo == null)
                return new DownloadResult(false, null, "No stream found");

            var filePath = Path.Combine(outputPath, $"{video.Id}.{streamInfo.Container.Name}");
            var fileProgress = new Progress<double>(p =>
                progress?.Report(new DownloadProgress(0, null, p, $"Downloading {video.Title}...")));

            await _client.Videos.Streams.DownloadAsync(streamInfo, filePath, fileProgress, ct);
            return new DownloadResult(true, filePath, null);
        }
        catch (OperationCanceledException)
        {
            return new DownloadResult(false, null, "Cancelled");
        }
        catch (Exception ex)
        {
            return new DownloadResult(false, null, $"YouTube: {ex.Message}");
        }
    }
}
