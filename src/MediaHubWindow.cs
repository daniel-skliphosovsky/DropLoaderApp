using Microsoft.Maui.Controls;

namespace MediaHub;

/// <summary>
/// Window that never resizes, minimizes or goes fullscreen. The shared part
/// only requests the platform chrome fix-up; each platform partial strips the
/// native controls that would let the user bypass the fixed size (resize
/// handles, minimize and fullscreen buttons) and syncs the native titlebar
/// with the app theme.
/// </summary>
public partial class MediaHubWindow : Window
{
    public MediaHubWindow(Page page) : base(page)
    {
        // The native window may not be attached yet when OnHandlerChanged
        // fires, so re-apply the chrome removal on activation too; the
        // operations are idempotent.
        Activated += (_, _) =>
        {
            LockPlatformChrome();
            ApplyThemeChrome();
        };

        // The header theme toggle flips UserAppTheme; keep the native
        // titlebar in sync so its text stays readable in both themes.
        if (Application.Current is { } app)
            app.RequestedThemeChanged += (_, _) => ApplyThemeChrome();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        LockPlatformChrome();
        ApplyThemeChrome();
    }

    partial void LockPlatformChrome();
    partial void ApplyThemeChrome();
}
