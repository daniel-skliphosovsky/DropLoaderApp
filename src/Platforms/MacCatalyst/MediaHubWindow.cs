using CoreGraphics;
using UIKit;

namespace MediaHub;

public partial class MediaHubWindow
{
    partial void LockPlatformChrome()
    {
        if (Handler?.PlatformView is not UIWindow uiWindow)
            return;

        var scene = uiWindow.WindowScene;
        if (scene?.SizeRestrictions is not { } restrictions)
            return;

        // Fix the size through the scene restrictions (the MAUI min/max clamp
        // maps to this too) and forbid fullscreen. macOS then disables the
        // resize and fullscreen controls; the minimize button is a Catalyst
        // limitation - no public API exposes it, AppKit bindings are not
        // shipped in the .NET 9 Catalyst workload.
        restrictions.MinimumSize = new CGSize(1280, 800);
        restrictions.MaximumSize = new CGSize(1280, 800);
        restrictions.AllowsFullScreen = false;
    }

    partial void ApplyThemeChrome()
    {
        if (Handler?.PlatformView is not UIWindow uiWindow)
            return;

        // The AppKit titlebar (close/minimize buttons, title) follows the
        // window's interface style. Without an explicit override it stays
        // light-on-light when the MAUI theme flips to Light, making the
        // title text unreadable. The UIKit override propagates to the
        // titlebar on Catalyst (AppKit types themselves are not exposed in
        // this workload).
        var isDark = Application.Current?.UserAppTheme == AppTheme.Dark;
        uiWindow.OverrideUserInterfaceStyle = isDark
            ? UIUserInterfaceStyle.Dark
            : UIUserInterfaceStyle.Light;
    }
}
