using Foundation;
using Microsoft.Maui;
using UIKit;

namespace MediaHub.MacCatalyst;

/// <summary>
/// Required for multi-window scenes: lets the red close button close the
/// window while the process (and the tray item) keeps running.
/// </summary>
[Register("SceneDelegate")]
public class SceneDelegate : MauiUISceneDelegate
{
}
