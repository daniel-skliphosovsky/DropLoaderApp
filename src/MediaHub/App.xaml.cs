using MediaHub.Services.Logging;
using Microsoft.Maui.Controls;

namespace MediaHub;

public partial class App : Application
{
    private static bool _errorHandlersInstalled;

    public App()
    {
        InitializeComponent();
        InstallErrorHandlers();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // The window opens at the minimum size and can be resized up to the
        // maximum; closing the window quits the app (standard MAUI behavior).
        return new Window(new AppShell())
        {
            Width = 900,
            Height = 620,
            MinimumWidth = 900,
            MinimumHeight = 620,
            MaximumWidth = 1160,
            MaximumHeight = 760
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
