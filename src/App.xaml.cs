using MediaHub.Services.Logging;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace MediaHub;

public partial class App : Application
{
    private static bool _errorHandlersInstalled;

    public App()
    {
        // The design is dark-first; the header toggle can still flip to light.
        UserAppTheme = AppTheme.Dark;
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
        // the minimize/fullscreen chrome on macOS and Windows.
        return new MediaHubWindow(new AppShell())
        {
            Width = 1280,
            Height = 800,
            MinimumWidth = 1280,
            MinimumHeight = 800,
            MaximumWidth = 1280,
            MaximumHeight = 800
        };
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
                    var page = Current?.Windows.FirstOrDefault()?.Page;
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
