namespace DropLoader;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell())
        {
            Width = 980,
            Height = 640,
            MinimumWidth = 780,
            MinimumHeight = 520
        };

        return window;
    }
}
