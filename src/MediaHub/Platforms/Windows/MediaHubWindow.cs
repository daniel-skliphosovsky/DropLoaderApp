using Microsoft.Maui.Platform;
using Microsoft.UI.Windowing;

namespace MediaHub;

public partial class MediaHubWindow
{
    partial void LockPlatformChrome()
    {
        if (Handler?.PlatformView is not MauiWinUIWindow winWindow)
            return;

        if (winWindow.AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
        }
    }

    // WinUI draws the titlebar from the app theme automatically, so there is
    // nothing extra to sync here.
    partial void ApplyThemeChrome() { }
}
