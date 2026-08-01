namespace MediaHub.Services.Interfaces;

/// <summary>
/// System tray integration: a persistent status-bar/tray icon with a menu,
/// close-to-tray window behavior and completion notifications.
/// </summary>
public interface ITrayService
{
    /// <summary>
    /// Creates the tray icon and menu. Safe to call once; subsequent calls are no-ops.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Links a MAUI window so the service can hook platform close events and
    /// bring the window back when the user asks for it.
    /// </summary>
    void AttachWindow(Window window);

    /// <summary>
    /// Makes the window visible and brings it to the front.
    /// </summary>
    void ShowWindow();

    /// <summary>
    /// Shows a best-effort system notification. Never throws.
    /// </summary>
    void ShowNotification(string title, string message);

    /// <summary>
    /// Updates the tray menu status item ("Downloading..." vs idle).
    /// </summary>
    void SetDownloading(bool isDownloading);

    /// <summary>
    /// Removes the tray icon and terminates the app.
    /// </summary>
    void ExitApplication();

    /// <summary>Raised when the user picks "Open" from the tray menu.</summary>
    event EventHandler? ShowRequested;

    /// <summary>Raised when the user picks "Quit" from the tray menu.</summary>
    event EventHandler? ExitRequested;
}
