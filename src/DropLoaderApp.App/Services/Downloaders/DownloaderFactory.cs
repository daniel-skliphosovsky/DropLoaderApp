using DropLoaderApp.Services.Interfaces;

namespace DropLoaderApp.Services.Downloaders;

public sealed class DownloaderFactory
{
    private readonly IEnumerable<IDownloader> _downloaders;

    public DownloaderFactory(IEnumerable<IDownloader> downloaders) => _downloaders = downloaders;

    public IDownloader? GetDownloader(string url) =>
        _downloaders.FirstOrDefault(d => d.CanHandle(url));

    public string GetPlatformName(string url)
    {
        var d = GetDownloader(url);
        return d?.PlatformName ?? "Unknown";
    }

    public bool CanDownload(string url) => GetDownloader(url) != null;
}
