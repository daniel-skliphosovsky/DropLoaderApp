using SoundCloudExplode;
using DropLoaderApp.Services.Interfaces;

namespace DropLoaderApp.Services.Downloaders;

public sealed class SoundCloudDownloader : IDownloader
{
    public string PlatformName => "SoundCloud";
    private readonly SoundCloudClient _client = new();

    public bool CanHandle(string url) =>
        !string.IsNullOrWhiteSpace(url) && url.Contains("soundcloud.com");

    public async Task<DownloadResult> DownloadAsync(
        string url, string outputPath,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        try
        {
            progress?.Report(new DownloadProgress(0, null, 0, "Fetching..."));

            var track = await _client.Tracks.GetAsync(url, ct);
            if (track == null)
                return new DownloadResult(false, null, "Track not found");

            var streamUrl = await _client.Tracks.GetDownloadUrlAsync(track, ct);
            if (string.IsNullOrEmpty(streamUrl))
                return new DownloadResult(false, null, "No stream URL");

            var filePath = Path.Combine(outputPath, $"{SanitizeFileName(track.Title)}.mp3");

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            using var response = await httpClient.GetAsync(streamUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            await using var fileStream = File.Create(filePath);

            var buffer = new byte[81920];
            long totalRead = 0;
            int read;

            while ((read = await stream.ReadAsync(buffer, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
                totalRead += read;
                progress?.Report(new DownloadProgress(totalRead, totalBytes,
                    totalBytes > 0 ? (double)totalRead / totalBytes : null, "Downloading..."));
            }

            return new DownloadResult(true, filePath, null);
        }
        catch (OperationCanceledException)
        {
            return new DownloadResult(false, null, "Cancelled");
        }
        catch (Exception ex)
        {
            return new DownloadResult(false, null, $"SoundCloud: {ex.Message}");
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }
}
