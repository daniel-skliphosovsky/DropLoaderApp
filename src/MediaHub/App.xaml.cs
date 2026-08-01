using MediaHub.Services.Logging;
using Microsoft.Maui.Controls;

namespace MediaHub;

public partial class App : Application
{
    private static bool _errorHandlersInstalled;

    public App()
    {
        // The design is dark-first; the header toggle can still flip to light.
        UserAppTheme = AppTheme.Dark;
        InitializeComponent();
        InstallErrorHandlers();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Fixed medium window: min == max == actual size locks resizing on
        // every platform; MediaHubWindow's platform partials additionally strip
        // the minimize/fullscreen chrome on macOS and Windows.
        return new MediaHubWindow(new AppShell())
        {
            Width = 1024,
            Height = 680,
            MinimumWidth = 1024,
            MinimumHeight = 680,
            MaximumWidth = 1024,
            MaximumHeight = 680
        };
    }

    private static void InstallErrorHandlers()
    {
        if (_errorHandlersInstalled)
            return;
        _errorHandlersInstalled = true;

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            OnUnhandledException(e.ExceptionObject as Exception ?? new Exception("Unknown fatal error"));

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
                            "Unexpected error",
                            $"Something went wrong: {exception.Message}\nDetails were written to the log file.",
                            "OK");
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
