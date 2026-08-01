using MediaHub.Services.Interfaces;

namespace MediaHub;

public partial class App : Application
{
    private readonly ITrayService _tray;
    private Window? _window;

    public App(ITrayService tray)
    {
        InitializeComponent();
        _tray = tray;

        _tray.ShowRequested += OnTrayShowRequested;
        _tray.ExitRequested += OnTrayExitRequested;
        _tray.Initialize();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        _window = CreateMainWindow();
        return _window;
    }

    private Window CreateMainWindow()
    {
        var window = new Window(new AppShell())
        {
            Width = 1040,
            Height = 680,
            MinimumWidth = 900,
            MinimumHeight = 620,
            MaximumWidth = 1160,
            MaximumHeight = 760
        };

        window.Destroying += (_, _) => _window = null;
        _tray.AttachWindow(window);
        return window;
    }

    private void OnTrayShowRequested(object? sender, EventArgs e)
    {
        if (_window is { } window)
        {
            ActivateWindow(window);
            _tray.ShowWindow();
        }
        else
        {
            // The previous window was closed to the tray; open a fresh one.
            OpenWindow(CreateMainWindow());
        }
    }

    private void OnTrayExitRequested(object? sender, EventArgs e)
    {
        _tray.ExitApplication();
    }
}
