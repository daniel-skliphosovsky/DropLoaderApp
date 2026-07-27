using CommunityToolkit.Maui;
using DropLoaderApp.Converters;
using DropLoaderApp.Services.Dialogs;
using DropLoaderApp.Services.Downloaders;
using DropLoaderApp.Services.Interfaces;
using DropLoaderApp.Services.Pickers;
using DropLoaderApp.ViewModels;
using DropLoaderApp.Views;
using Microsoft.Extensions.Logging;
using TikTokExplode.Extensions;

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

        // Converters
        builder.Services.AddSingleton<InverseBoolConverter>();
        builder.Services.AddSingleton<BoolToStringConverter>();
        builder.Services.AddSingleton<IntToThemeConverter>();
        builder.Services.AddSingleton<StringNotEmptyConverter>();

        // Services
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<IFolderPickerService, FolderPickerService>();
        builder.Services.AddSingleton<DownloaderFactory>();

        // TikTok — registers ITikTokClient and all infrastructure
        builder.Services.AddTikTokExplode();

        // Downloaders
        builder.Services.AddTransient<IDownloader, TikTokDownloader>();
        builder.Services.AddTransient<IDownloader, YouTubeDownloader>();
        builder.Services.AddTransient<IDownloader, SoundCloudDownloader>();

        // ViewModels
        builder.Services.AddTransient<MainViewModel>();

        // Pages
        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
