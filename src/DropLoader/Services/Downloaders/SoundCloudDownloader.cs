using DropLoader.Services.Interfaces;
using SoundCloudExplode;

namespace DropLoader.Services.Downloaders;

public sealed class SoundCloudDownloader : IDownloader
{
    public string PlatformName => "SoundCloud";

    private readonly HttpClient _http;
    private readonly SoundCloudClient _client;

    public SoundCloudDownloader(HttpClient httpClient)
    {
        _http = httpClient;
        _client = new SoundCloudClient(httpClient);
    }

    public bool CanHandle(string url) =>
        UrlHelpers.UrlBelongsTo(url, "soundcloud.com", "on.soundcloud.com");

    public async Task<DownloadResult> DownloadAsync(
        string url, string outputPath,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        string? filePath = null;
        try
        {
            progress?.Report(new DownloadProgress(0, null, 0, "Fetching..."));

            var track = await _client.Tracks.GetAsync(url, ct);
            if (track == null)
                return new DownloadResult(false, null, "Track not found");

            var streamUrl = await _client.Tracks.GetDownloadUrlAsync(track, ct);
            if (string.IsNullOrEmpty(streamUrl))
                return new DownloadResult(false, null, "No stream URL");

            filePath = Path.Combine(outputPath, $"{SanitizeFileName(track.Title ?? "track")}.mp3");

            using var response = await _http.GetAsync(streamUrl, HttpCompletionOption.ResponseHeadersRead, ct);
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
            TryDeleteFile(filePath);
            return new DownloadResult(false, null, "Cancelled");
        }
        catch (Exception ex)
        {
            return new DownloadResult(false, null, $"SoundCloud: {ex.Message}");
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

    private static string SanitizeFileName(string name) =>
        string.Join("_", name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries))
            .Trim().TrimEnd('.', ' ');
}
