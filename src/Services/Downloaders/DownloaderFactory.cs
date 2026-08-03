using MediaHub.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace MediaHub.Services.Downloaders;

public sealed class DownloaderFactory
{
    private readonly IServiceProvider _services;

    public DownloaderFactory(IServiceProvider services) => _services = services;

    public IDownloader? GetDownloader(string url) =>
        _services.GetServices<IDownloader>().FirstOrDefault(d => d.CanHandle(url));

    public bool CanDownload(string url) => GetDownloader(url) != null;
}
