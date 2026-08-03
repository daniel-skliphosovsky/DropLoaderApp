using MediaHub.Services.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace MediaHub;

public partial class App : Application
{
    private static bool _errorHandlersInstalled;

    public App()
    {
        // Follow the OS theme at startup (Unspecified defaults to dark, the
        // design's headline); the header toggle still flips UserAppTheme and
        // overrides whatever was picked here.
        UserAppTheme = AppInfo.RequestedTheme == AppTheme.Light ? AppTheme.Light : AppTheme.Dark;
        InitializeComponent();

        // Russian is the default UI language; the header toggle switches to
        // English and the choice is persisted across launches.
        Loc.SetLanguage(Preferences.Default.Get(Loc.LanguagePreferenceKey, "ru"));

        InstallErrorHandlers();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Fixed medium window: min == max == actual size locks resizing on
        // every platform; MediaHubWindow's platform partials additionally strip
        // the minimize/fullscreen chrome on macOS and Windows. Windows runs in
        // a smaller fixed window because its DPI scaling makes 1280x800 feel
        // oversized; macOS keeps the ideal 1280x800.
        var window = new MediaHubWindow(new AppShell());
#if WINDOWS
        window.Width = 1024;
        window.Height = 680;
        window.MinimumWidth = 1024;
        window.MinimumHeight = 680;
        window.MaximumWidth = 1024;
        window.MaximumHeight = 680;
#else
        window.Width = 1280;
        window.Height = 800;
        window.MinimumWidth = 1280;
        window.MinimumHeight = 800;
        window.MaximumWidth = 1280;
        window.MaximumHeight = 800;
#endif
        return window;
    }

    private static void InstallErrorHandlers()
    {
        if (_errorHandlersInstalled)
            return;
        _errorHandlersInstalled = true;

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            OnUnhandledException(e.ExceptionObject as Exception ?? new Exception(Loc.Get(LocKeys.DialogFatal)));

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            OnUnhandledException(e.Exception);
            e.SetObserved();
        };
    }

    private static void OnUnhandledException(Exception exception)
    {
        AppLogger.Log(exception);

        // The dialog is best-effort (the exception may be fatal); the full
        // details always land in the log file.
        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    Page? page = Current?.Windows.FirstOrDefault()?.Page;
                    if (page is not null)
                    {
                        _ = page.DisplayAlert(
                            Loc.Get(LocKeys.DialogUnexpectedTitle),
                            Loc.Get(LocKeys.DialogUnexpectedMessage, exception.Message),
                            Loc.Get(LocKeys.DialogOk));
                    }
                }
                catch
                {
                    // Never throw from an error handler.
                }
            });
        }
        catch
        {
            // Never throw from an error handler.
        }
    }
}
