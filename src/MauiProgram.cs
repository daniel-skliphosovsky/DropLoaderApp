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
        MauiAppBuilder builder = MauiApp.CreateBuilder();
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
        {
            if (handler.PlatformView is not Microsoft.UI.Xaml.Controls.TextBox textBox)
                return;

            // The styled container draws the outline, so drop the native border.
            textBox.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);

            // The default WinUI TextBox template still paints an underline
            // for the Focused (accent color), PointerOver (gray) and
            // Disabled (gray) states. Zero them all locally so only the
            // styled container shows through; the background stays
            // transparent and the padding is untouched.
            textBox.Resources["TextControlBorderBrushFocused"] =
                new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
            textBox.Resources["TextControlBorderThemeThicknessFocused"] =
                new Microsoft.UI.Xaml.Thickness(0);
            textBox.Resources["TextControlBorderBrushPointerOver"] =
                new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
            textBox.Resources["TextControlBorderThemeThicknessPointerOver"] =
                new Microsoft.UI.Xaml.Thickness(0);
            textBox.Resources["TextControlBorderBrushDisabled"] =
                new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
            textBox.Resources["TextControlBorderThemeThicknessDisabled"] =
                new Microsoft.UI.Xaml.Thickness(0);
        });
#elif MACCATALYST
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("MediaHubBorderlessEntry", (handler, _) =>
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None);
#endif

        // Single HttpClient (with ForcedUserAgentHandler) shared by the
        // YouTube, SoundCloud, VK and Scrape downloaders. The modern browser
        // User-Agent is forced on every request here, not just set as a
        // default: Explode libraries stamp their own stale UA onto the
        // request headers during SendAsync, which would otherwise beat the
        // DefaultRequestHeaders value. TikTok is excluded: it registers its
        // own clean HttpClient below.
        builder.Services.AddSingleton(_ =>
        {
            HttpClient http = new HttpClient(new ForcedUserAgentHandler())
            {
                Timeout = TimeSpan.FromMinutes(5)
            };
            return http;
        });

        // TikTokClient takes ownership of a dedicated HttpClient (no
        // ForcedUserAgentHandler): TikTokExplode rotates a fresh random
        // User-Agent on every API retry to get past api22 rate limiting, and
        // the shared forced-agent handler would stamp one fixed UA on all 30
        // attempts, making every request hit the 429 rate limit.
        builder.Services.AddSingleton(sp =>
            new TikTokClient(
                new HttpClient { Timeout = TimeSpan.FromMinutes(5) },
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
        builder.Services.AddTransient<IDownloader, OkDownloader>();

        // ViewModels and Pages
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}

/// <summary>
/// Forces the modern browser User-Agent on every request. SoundCloudExplode
/// and similar libraries overwrite the client default with their own stale
/// agent inside SendAsync; stamping it here guarantees the shared value wins.
/// </summary>
internal sealed class ForcedUserAgentHandler : HttpMessageHandler
{
    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";

    private readonly HttpMessageInvoker _inner = new(new SocketsHttpHandler());

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Remove("User-Agent");
        request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
        return await _inner.SendAsync(request, cancellationToken);
    }
}
