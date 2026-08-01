using CommunityToolkit.Maui;
using DropLoaderApp.Services.Dialogs;
using DropLoaderApp.Services.Downloaders;
using DropLoaderApp.Services.Interfaces;
using DropLoaderApp.Services.Pickers;
using DropLoaderApp.ViewModels;
using DropLoaderApp.Views;
using Microsoft.Extensions.Logging;
using TikTokExplode;

namespace DropLoaderApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Single HttpClient shared by all downloaders and underlying clients.
        builder.Services.AddSingleton(new HttpClient { Timeout = TimeSpan.FromMinutes(5) });

        // TikTokClient takes ownership of no resources here (it was given the
        // shared HttpClient), so a plain singleton is fine. The options pin the
        // timeout to the same 5 minutes, otherwise the library would override it.
        builder.Services.AddSingleton(sp =>
            new TikTokClient(
                sp.GetRequiredService<HttpClient>(),
                new TikTokClientOptions { TimeoutSeconds = 300 }));

        // Services
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<IFolderPickerService, FolderPickerService>();
        builder.Services.AddSingleton<DownloaderFactory>();

        // Downloaders (transient, resolved through the factory)
        builder.Services.AddTransient<IDownloader, TikTokDownloader>();
        builder.Services.AddTransient<IDownloader, YouTubeDownloader>();
        builder.Services.AddTransient<IDownloader, SoundCloudDownloader>();

        // ViewModels and Pages
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
