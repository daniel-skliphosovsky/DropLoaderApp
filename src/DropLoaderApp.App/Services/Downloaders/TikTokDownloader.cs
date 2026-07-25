using TikTokExplode;
using TikTokExplode.Domain.Enums;
using TikTokExplode.Extensions;
using Microsoft.Extensions.DependencyInjection;
using DropLoaderApp.Services.Interfaces;

namespace DropLoaderApp.Services.Downloaders;

public sealed class TikTokDownloader : IDownloader
{
    public string PlatformName => "TikTok";
    private readonly TikTokClient _client;

    public TikTokDownloader()
    {
        var provider = new ServiceCollection()
            .AddTikTokExplode()
            .BuildServiceProvider();
        _client = provider.GetRequiredService<TikTokClient>();
    }

    public bool CanHandle(string url) =>
        !string.IsNullOrWhiteSpace(url) &&
        (url.Contains("tiktok.com") || url.Contains("vm.tiktok.com"));

    public async Task<DownloadResult> DownloadAsync(
        string url, string outputPath,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        try
        {
            var publication = await _client.GetPublicationAsync(url, ct);
            progress?.Report(new DownloadProgress(0, null, 0, "Fetching..."));

            if (publication.Type == PublicationType.Video && publication.Video != null)
            {
                var videoPath = Path.Combine(outputPath, $"{publication.Id}_video.mp4");
                var fileProgress = new Progress<long>(bytes =>
                    progress?.Report(new DownloadProgress(bytes, null, null, "Downloading video...")));

                await _client.DownloadVideoAsync(publication.Video.PlayUrl, videoPath, fileProgress, ct);
                return new DownloadResult(true, videoPath, null);
            }

            if (publication.Images != null && publication.Images.Count > 0)
            {
                var dirPath = Path.Combine(outputPath, $"{publication.Id}_images");
                Directory.CreateDirectory(dirPath);
                var imageProgress = new Progress<double>(p =>
                    progress?.Report(new DownloadProgress(0, publication.Images.Count, p, "Downloading images...")));

                await _client.DownloadImagesAsync(publication.Images, dirPath, imageProgress, ct);
                return new DownloadResult(true, dirPath, null);
            }

            return new DownloadResult(false, null, "No downloadable content");
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
}
