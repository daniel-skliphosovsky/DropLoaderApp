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
        // The window starts at its minimum size and can be resized freely all
        // the way up to fullscreen (no maximum constraint), so the user can
        // expand it with the green button or by dragging the edges.
        return new Window(new AppShell())
        {
            Width = 800,
            Height = 560,
            MinimumWidth = 800,
            MinimumHeight = 560,
            MaximumWidth = 4000,
            MaximumHeight = 3000
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
