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
        restrictions.MinimumSize = new CGSize(1024, 680);
        restrictions.MaximumSize = new CGSize(1024, 680);
        restrictions.AllowsFullScreen = false;
    }
}
