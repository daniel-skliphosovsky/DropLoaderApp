using AppKit;
using CoreGraphics;
using Foundation;
using MediaHub.Services.Interfaces;
using Microsoft.Maui.Controls;
using ObjCRuntime;
using System.Runtime.InteropServices;
using UIKit;
using UserNotifications;

namespace MediaHub.MacCatalyst;

/// <summary>
/// Menu bar integration for macOS Catalyst.
///
/// The .NET Catalyst bindings do not expose NSStatusBar/NSMenu/NSMenuItem and
/// ObjCRuntime.Messaging is internal, so the status item is created through
/// direct objc_msgSend calls (the same approach as Drastic.Interop). AppKit is
/// a real framework on Catalyst, so these standard selectors exist at runtime.
///
/// Window behavior: the app opts into multi-window scenes (see Info.plist),
/// so clicking the red close button closes the window while the process keeps
/// running with the status item still present. Reopening creates a fresh
/// window through the normal scene flow.
/// </summary>
public sealed class TrayService : ITrayService
{
    private const double VariableStatusItemLength = -1;

    private static readonly IntPtr ClsStatusBar = Class.GetHandle("NSStatusBar");
    private static readonly IntPtr ClsMenu = Class.GetHandle("NSMenu");
    private static readonly IntPtr ClsMenuItem = Class.GetHandle("NSMenuItem");

    private static readonly IntPtr SelSystemStatusBar = Selector.GetHandle("systemStatusBar");
    private static readonly IntPtr SelStatusItemWithLength = Selector.GetHandle("statusItemWithLength:");
    private static readonly IntPtr SelAlloc = Selector.GetHandle("alloc");
    private static readonly IntPtr SelInit = Selector.GetHandle("init");
    private static readonly IntPtr SelInitMenuItem = Selector.GetHandle("initWithTitle:action:keyEquivalent:");
    private static readonly IntPtr SelSeparatorItem = Selector.GetHandle("separatorItem");
    private static readonly IntPtr SelSetImage = Selector.GetHandle("setImage:");
    private static readonly IntPtr SelSetMenu = Selector.GetHandle("setMenu:");
    private static readonly IntPtr SelSetToolTip = Selector.GetHandle("setToolTip:");
    private static readonly IntPtr SelSetVisible = Selector.GetHandle("setVisible:");
    private static readonly IntPtr SelSetTarget = Selector.GetHandle("setTarget:");
    private static readonly IntPtr SelSetEnabled = Selector.GetHandle("setEnabled:");
    private static readonly IntPtr SelSetTitle = Selector.GetHandle("setTitle:");
    private static readonly IntPtr SelAddItem = Selector.GetHandle("addItem:");
    private static readonly IntPtr SelRemoveStatusItem = Selector.GetHandle("removeStatusItem:");

    private readonly TrayTarget _target = new();

    private NSObject? _statusItem;
    private NSObject? _statusMenuItem;
    private Window? _mauiWindow;
    private bool _initialized;

    private static readonly UNUserNotificationCenter NotificationCenter =
        UNUserNotificationCenter.Current;

    private static readonly NotificationDelegate NotificationHandler = new();
    private static bool _notificationsReady;

    public event EventHandler? ShowRequested;
    public event EventHandler? ExitRequested;

    public void Initialize()
    {
        if (_initialized)
            return;
        _initialized = true;

        MainThread.BeginInvokeOnMainThread(CreateStatusItem);
        PrepareNotifications();
    }

    public void AttachWindow(Window window)
    {
        _mauiWindow = window;
    }

    public void ShowWindow()
    {
        if (_mauiWindow?.Handler?.PlatformView is UIWindow uiWindow)
            uiWindow.MakeKeyAndVisible();
    }

    public void ShowNotification(string title, string message)
    {
        if (!_notificationsReady)
            return;

        _ = PostNotificationAsync(title, message);
    }

    public void SetDownloading(bool isDownloading)
    {
        if (_statusMenuItem is null)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            SetTitle(_statusMenuItem, isDownloading ? "Downloading..." : "Idle");
            ObjC.SendBool(_statusMenuItem.Handle, SelSetEnabled, false);
        });
    }

    public void ExitApplication()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (_statusItem is not null)
                {
                    var statusBar = Runtime.GetNSObject(
                        ObjC.Send(ClsStatusBar, SelSystemStatusBar));
                    if (statusBar is not null)
                        ObjC.SendVoid(statusBar.Handle, SelRemoveStatusItem, _statusItem.Handle);
                }
            }
            catch
            {
            }

            Environment.Exit(0);
        });
    }

    private void CreateStatusItem()
    {
        try
        {
            var statusBar = Runtime.GetNSObject(
                ObjC.Send(ClsStatusBar, SelSystemStatusBar));
            var item = Runtime.GetNSObject(ObjC.SendDouble(
                statusBar!.Handle, SelStatusItemWithLength, VariableStatusItemLength));
            if (item is null)
                return;

            ObjC.SendVoid(item.Handle, SelSetImage, CreateTemplateIcon().Handle);
            ObjC.SendVoid(item.Handle, SelSetToolTip, new NSString("MediaHub").Handle);
            ObjC.SendVoid(item.Handle, SelSetMenu, BuildMenu().Handle);
            ObjC.SendBool(item.Handle, SelSetVisible, true);

            _statusItem = item;
        }
        catch
        {
            // The tray is best-effort; the app keeps working without it.
        }
    }

    private NSObject BuildMenu()
    {
        var menu = Runtime.GetNSObject(ObjC.Send(ObjC.Send(ClsMenu, SelAlloc), SelInit));
        if (menu is null)
            throw new InvalidOperationException("Failed to create NSMenu");

        _target.Open = () => ShowRequested?.Invoke(this, EventArgs.Empty);
        _target.Quit = () => ExitRequested?.Invoke(this, EventArgs.Empty);

        ObjC.SendVoid(menu.Handle, SelAddItem,
            CreateMenuItem("Open MediaHub", "openMediaHub:").Handle);
        _statusMenuItem = CreateMenuItem("Idle", null);
        ObjC.SendBool(_statusMenuItem.Handle, SelSetEnabled, false);
        ObjC.SendVoid(menu.Handle, SelAddItem, _statusMenuItem.Handle);

        var separator = Runtime.GetNSObject(ObjC.Send(ClsMenuItem, SelSeparatorItem));
        if (separator is not null)
            ObjC.SendVoid(menu.Handle, SelAddItem, separator.Handle);

        ObjC.SendVoid(menu.Handle, SelAddItem,
            CreateMenuItem("Quit MediaHub", "quitMediaHub:").Handle);

        return menu;
    }

    private NSObject CreateMenuItem(string title, string? action)
    {
        var instance = ObjC.Send(ClsMenuItem, SelAlloc);
        var selector = action is null ? IntPtr.Zero : Selector.GetHandle(action);
        var item = Runtime.GetNSObject(ObjC.Send3(
            instance, SelInitMenuItem, new NSString(title).Handle, selector, IntPtr.Zero));
        if (item is null)
            throw new InvalidOperationException("Failed to create NSMenuItem");

        if (action is not null)
            ObjC.SendVoid(item.Handle, SelSetTarget, _target.Handle);

        return item;
    }

    private static void SetTitle(NSObject item, string title)
        => ObjC.SendVoid(item.Handle, SelSetTitle, new NSString(title).Handle);

    /// <summary>
    /// Draws a simple down-arrow and marks it as a template so the system
    /// tints it for light/dark menu bars.
    /// </summary>
    private static NSImage CreateTemplateIcon()
    {
        var size = new CGSize(18, 18);
        var renderer = new UIGraphicsImageRenderer(size);
        var uiImage = renderer.CreateImage(ctx =>
        {
            var path = new UIBezierPath();
            path.MoveTo(new CGPoint(9, 14));
            path.AddLineTo(new CGPoint(2, 7));
            path.AddLineTo(new CGPoint(5.5f, 7));
            path.AddLineTo(new CGPoint(5.5f, 2));
            path.AddLineTo(new CGPoint(12.5f, 2));
            path.AddLineTo(new CGPoint(12.5f, 7));
            path.AddLineTo(new CGPoint(16, 7));
            path.ClosePath();
            UIColor.White.SetFill();
            path.Fill();
        });

        return new NSImage(uiImage.CGImage!, size) { Template = true };
    }

    private static async void PrepareNotifications()
    {
        try
        {
            NotificationCenter.Delegate = NotificationHandler;
            var (granted, _) = await NotificationCenter.RequestAuthorizationAsync(
                UNAuthorizationOptions.Alert);
            _notificationsReady = granted;
        }
        catch
        {
            _notificationsReady = false;
        }
    }

    private static async Task PostNotificationAsync(string title, string message)
    {
        try
        {
            var content = new UNMutableNotificationContent
            {
                Title = title,
                Body = message
            };

            var request = UNNotificationRequest.FromIdentifier(
                Guid.NewGuid().ToString("N"), content, null);

            await NotificationCenter.AddNotificationRequestAsync(request);
        }
        catch
        {
            // Notifications are best-effort; never crash the download flow.
        }
    }

    [Register("MediaHubTrayTarget")]
    internal sealed class TrayTarget : NSObject
    {
        public Action? Open;
        public Action? Quit;

        [Export("openMediaHub:")]
        public void OpenMediaHub(NSObject sender) => Open?.Invoke();

        [Export("quitMediaHub:")]
        public void QuitMediaHub(NSObject sender) => Quit?.Invoke();
    }

    private sealed class NotificationDelegate : UNUserNotificationCenterDelegate
    {
        public override void WillPresentNotification(
            UNUserNotificationCenter center,
            UNNotification notification,
            Action<UNNotificationPresentationOptions> completionHandler)
        {
#pragma warning disable CA1422 // The Catalyst binding only exposes the deprecated Alert option.
            completionHandler(UNNotificationPresentationOptions.Alert);
#pragma warning restore CA1422
        }
    }

    /// <summary>
    /// Minimal objc_msgSend surface needed for the status item. AppKit is
    /// loaded on Catalyst, so these standard selectors are available.
    /// </summary>
    private static class ObjC
    {
        private const string LibObjc = "/usr/lib/libobjc.dylib";

        [DllImport(LibObjc, EntryPoint = "objc_msgSend")]
        public static extern IntPtr Send(IntPtr receiver, IntPtr selector);

        [DllImport(LibObjc, EntryPoint = "objc_msgSend")]
        public static extern IntPtr SendDouble(IntPtr receiver, IntPtr selector, double value);

        [DllImport(LibObjc, EntryPoint = "objc_msgSend")]
        public static extern IntPtr Send3(
            IntPtr receiver, IntPtr selector, IntPtr a, IntPtr b, IntPtr c);

        [DllImport(LibObjc, EntryPoint = "objc_msgSend")]
        public static extern void SendVoid(IntPtr receiver, IntPtr selector, IntPtr a);

        [DllImport(LibObjc, EntryPoint = "objc_msgSend")]
        public static extern void SendBool(
            IntPtr receiver, IntPtr selector, [MarshalAs(UnmanagedType.I1)] bool value);
    }
}
