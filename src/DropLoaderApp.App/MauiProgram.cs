using CommunityToolkit.Maui;
using DropLoaderApp.Services.Dialogs;
using DropLoaderApp.Services.Downloaders;
using DropLoaderApp.Services.Interfaces;
using DropLoaderApp.Services.Pickers;
using DropLoaderApp.ViewModels;
using DropLoaderApp.Views;
using Microsoft.Extensions.Logging;

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

        // Services
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<IFolderPickerService, FolderPickerService>();
        builder.Services.AddSingleton<DownloaderFactory>();

        // Downloaders
        builder.Services.AddTransient<IDownloader, TikTokDownloader>();
        builder.Services.AddTransient<IDownloader, YouTubeDownloader>();
        builder.Services.AddTransient<IDownloader, SoundCloudDownloader>();

        // ViewModels
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<DownloadViewModel>();

        // Pages
        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
