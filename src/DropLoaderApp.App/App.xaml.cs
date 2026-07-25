namespace DropLoaderApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());

        // Fixed window size
        window.Width = 680;
        window.Height = 480;
        window.MinimumWidth = 680;
        window.MinimumHeight = 480;
        window.MaximumWidth = 680;
        window.MaximumHeight = 480;

        return window;
    }
}
