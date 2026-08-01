using CommunityToolkit.Maui;
using MediaHub.Services.Dialogs;
using MediaHub.Services.Downloaders;
using MediaHub.Services.Interfaces;
using MediaHub.Services.Pickers;
using MediaHub.ViewModels;
using MediaHub.Views;
using Microsoft.Extensions.Logging;
using TikTokExplode;

#if MACCATALYST
using Microsoft.Maui.LifecycleEvents;
#endif

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

#if MACCATALYST
        // The window is size-constrained (see App.CreateWindow). Keep the green
        // button from pushing the app into fullscreen - it becomes a plain zoom.
        builder.ConfigureLifecycleEvents(events =>
            events.AddiOS(iOS => iOS.OnActivated(_ =>
            {
                // AllowsFullScreen exists since macOS 16; on older systems the
                // green button is simply left alone.
                if (!OperatingSystem.IsIOSVersionAtLeast(16))
                    return;

                foreach (var scene in (UIKit.UIApplication.SharedApplication?.ConnectedScenes ?? [])
                             .OfType<UIKit.UIWindowScene>())
                {
                    if (scene.SizeRestrictions is { } restrictions)
                        restrictions.AllowsFullScreen = false;
                }
            })));
#endif

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
