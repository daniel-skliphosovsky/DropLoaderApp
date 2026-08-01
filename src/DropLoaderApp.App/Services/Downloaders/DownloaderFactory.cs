using DropLoaderApp.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DropLoaderApp.Services.Downloaders;

public sealed class DownloaderFactory
{
    private readonly IServiceProvider _services;

    public DownloaderFactory(IServiceProvider services) => _services = services;

    public IDownloader? GetDownloader(string url) =>
        _services.GetServices<IDownloader>().FirstOrDefault(d => d.CanHandle(url));

    public string GetPlatformName(string url)
    {
        var d = GetDownloader(url);
        return d?.PlatformName ?? "Unknown";
    }

    public bool CanDownload(string url) => GetDownloader(url) != null;
}
