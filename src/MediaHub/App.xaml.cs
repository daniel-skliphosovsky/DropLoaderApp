namespace MediaHub;

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
            Width = 1040,
            Height = 680,
            MinimumWidth = 900,
            MinimumHeight = 620,
            MaximumWidth = 1160,
            MaximumHeight = 760
        };

        return window;
    }
}
