using System.Runtime.InteropServices;
using MediaHub.Services.Interfaces;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;
using Microsoft.UI.Windowing;
using WinRT.Interop;

namespace MediaHub.WinUI;

/// <summary>
/// System tray integration for Windows via Shell_NotifyIcon (works for
/// unpackaged apps, unlike Toast notifications). Window close is intercepted
/// through AppWindow.Closing so the process keeps running in the tray.
/// </summary>
public sealed class TrayService : ITrayService
{
    private const uint WM_APP = 0x8000;
    private const uint WM_RBUTTONUP = 0x0205;
    private const uint WM_CONTEXTMENU = 0x007B;

    private const uint NIM_ADD = 0x00000000;
    private const uint NIM_MODIFY = 0x00000001;
    private const uint NIM_DELETE = 0x00000002;
    private const uint NIM_SETVERSION = 0x00000004;

    private const uint NIF_MESSAGE = 0x00000001;
    private const uint NIF_ICON = 0x00000002;
    private const uint NIF_TIP = 0x00000004;
    private const uint NIF_INFO = 0x00000010;

    private const uint NIIF_INFO = 0x00000001;
    private const uint NOTIFYICON_VERSION_4 = 4;
    private const int IDI_APPLICATION = 32512;

    private const uint MF_STRING = 0x00000000;
    private const uint MF_GRAYED = 0x00000001;
    private const uint MF_DISABLED = 0x00000002;
    private const uint MF_SEPARATOR = 0x00000800;

    private const uint TPM_RIGHTBUTTON = 0x00000002;
    private const uint TPM_TOPALIGN = 0x00000000;
    private const uint TPM_RETURNCMD = 0x00000100;

    private const uint ID_OPEN = 1;
    private const uint ID_STATUS = 2;
    private const uint ID_QUIT = 3;

    private const int TrayIconId = 1;
    private const uint TrayCallbackMessage = WM_APP + 1;
    private const string TrayWindowClass = "MediaHubTrayWindow";

    private Window? _mauiWindow;
    private MauiWinUIWindow? _nativeWindow;
    private AppWindow? _appWindow;
    private IntPtr _hwnd;
    private bool _isDownloading;
    private bool _iconAdded;

    private readonly WndProcDelegate _wndProc;

    public TrayService()
    {
        _wndProc = WndProc;
    }

    public event EventHandler? ShowRequested;
    public event EventHandler? ExitRequested;

    public void Initialize()
    {
        CreateTrayWindow();
        AddIcon();
    }

    public void AttachWindow(Window window)
    {
        _mauiWindow = window;
        window.Created += OnWindowCreated;
    }

    public void ShowWindow()
    {
        if (_nativeWindow is null)
            return;

        _appWindow?.Show();
        var hwnd = WindowNative.GetWindowHandle(_nativeWindow);
        NativeMethods.ShowWindow(hwnd, 5); // SW_SHOW
        _appWindow?.MoveInZOrderAtTop();
        NativeMethods.SetForegroundWindow(hwnd);
    }

    public void ShowNotification(string title, string message)
    {
        try
        {
            if (!_iconAdded || _hwnd == IntPtr.Zero)
                return;

            var nid = new NOTIFYICONDATA
            {
                cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _hwnd,
                uID = TrayIconId,
                uFlags = NIF_INFO,
                szInfo = message,
                szInfoTitle = title,
                dwInfoFlags = NIIF_INFO
            };

            NativeMethods.Shell_NotifyIcon(NIM_MODIFY, ref nid);
        }
        catch
        {
            // Notifications are best-effort; never crash the download flow.
        }
    }

    public void SetDownloading(bool isDownloading)
    {
        _isDownloading = isDownloading;
    }

    public void ExitApplication()
    {
        try
        {
            if (_iconAdded && _hwnd != IntPtr.Zero)
            {
                var nid = new NOTIFYICONDATA
                {
                    cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
                    hWnd = _hwnd,
                    uID = TrayIconId
                };

                NativeMethods.Shell_NotifyIcon(NIM_DELETE, ref nid);
            }
        }
        catch
        {
        }

        Environment.Exit(0);
    }

    private void CreateTrayWindow()
    {
        if (_hwnd != IntPtr.Zero)
            return;

        var hInstance = NativeMethods.GetModuleHandle(null);

        var wndClass = new WNDCLASS
        {
            lpfnWndProc = _wndProc,
            hInstance = hInstance,
            lpszClassName = TrayWindowClass
        };
        NativeMethods.RegisterClass(ref wndClass);

        // Message-only window; receives the Shell_NotifyIcon callback messages.
        _hwnd = NativeMethods.CreateWindowEx(
            0, TrayWindowClass, "MediaHub", 0, 0, 0, 0, 0,
            new IntPtr(-3), IntPtr.Zero, hInstance, IntPtr.Zero);
    }

    private void AddIcon()
    {
        if (_iconAdded || _hwnd == IntPtr.Zero)
            return;

        var nid = new NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _hwnd,
            uID = TrayIconId,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = TrayCallbackMessage,
            hIcon = NativeMethods.LoadIcon(IntPtr.Zero, (IntPtr)IDI_APPLICATION),
            szTip = "MediaHub"
        };

        NativeMethods.Shell_NotifyIcon(NIM_ADD, ref nid);

        // Version 4 gives us modern event messages and balloon behavior.
        nid.uFlags = 0;
        nid.uTimeoutOrVersion = NOTIFYICON_VERSION_4;
        NativeMethods.Shell_NotifyIcon(NIM_SETVERSION, ref nid);

        _iconAdded = true;
    }

    private void OnWindowCreated(object? sender, EventArgs e)
    {
        if (_mauiWindow?.Handler?.PlatformView is not MauiWinUIWindow native)
            return;

        _nativeWindow = native;
        _appWindow = native.AppWindow;

        if (_appWindow is not null)
            _appWindow.Closing += OnAppWindowClosing;
    }

    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        // Close request hides the window instead of terminating the app.
        args.Cancel = true;
        _appWindow?.Hide();
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == TrayCallbackMessage && (uint)lParam is WM_RBUTTONUP or WM_CONTEXTMENU)
        {
            ShowContextMenu();
            return IntPtr.Zero;
        }

        return NativeMethods.DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private void ShowContextMenu()
    {
        var menu = NativeMethods.CreatePopupMenu();
        if (menu == IntPtr.Zero)
            return;

        NativeMethods.AppendMenu(menu, MF_STRING, ID_OPEN, "Open MediaHub");
        NativeMethods.AppendMenu(
            menu, MF_STRING | MF_GRAYED | MF_DISABLED, ID_STATUS,
            _isDownloading ? "Downloading..." : "Idle");
        NativeMethods.AppendMenu(menu, MF_SEPARATOR, 0, null);
        NativeMethods.AppendMenu(menu, MF_STRING, ID_QUIT, "Quit");

        NativeMethods.GetCursorPos(out var pt);
        var command = NativeMethods.TrackPopupMenu(
            menu,
            TPM_RETURNCMD | TPM_RIGHTBUTTON | TPM_TOPALIGN,
            pt.X, pt.Y, 0, _hwnd, IntPtr.Zero);

        NativeMethods.DestroyMenu(menu);

        switch (command)
        {
            case ID_OPEN:
                ShowRequested?.Invoke(this, EventArgs.Empty);
                break;
            case ID_QUIT:
                ExitRequested?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASS
    {
        public uint style;
        public WndProcDelegate lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszClassName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private static class NativeMethods
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool RegisterClass(ref WNDCLASS lpWndClass);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateWindowEx(
            uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
            int x, int y, int nWidth, int nHeight,
            IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr GetModuleHandle(string? lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string? lpNewItem);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern uint TrackPopupMenu(
            IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

        [DllImport("user32.dll")]
        public static extern bool DestroyMenu(IntPtr hMenu);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
