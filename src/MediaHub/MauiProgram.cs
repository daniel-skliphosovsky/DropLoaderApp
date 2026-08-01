using CommunityToolkit.Maui;
using MediaHub.Services.Dialogs;
using MediaHub.Services.Downloaders;
using MediaHub.Services.Interfaces;
using MediaHub.Services.Pickers;
using MediaHub.ViewModels;
using MediaHub.Views;
using Microsoft.Extensions.Logging;
using TikTokExplode;

namespace MediaHub;

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

        // Entry: the styled container already draws the outline, so drop the
        // native border/underline the platform adds on top of it.
#if WINDOWS
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("MediaHubBorderlessEntry", (handler, _) =>
            handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0));
#elif MACCATALYST
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("MediaHubBorderlessEntry", (handler, _) =>
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None);
#endif

        // Single HttpClient shared by all downloaders and underlying clients.
        // The modern browser User-Agent is set here once so every downloader
        // (including the Explode libraries) sends the same header; a stale or
        // generic agent gets the media APIs to reject requests with 401/400.
        builder.Services.AddSingleton(_ =>
        {
            var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
            return http;
        });

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
        builder.Services.AddTransient<IDownloader, VkDownloader>();

        // ViewModels and Pages
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
