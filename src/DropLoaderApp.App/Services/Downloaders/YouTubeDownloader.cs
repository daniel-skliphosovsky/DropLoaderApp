using DropLoaderApp.Services.Interfaces;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace DropLoaderApp.Services.Downloaders;

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
