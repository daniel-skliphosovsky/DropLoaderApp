using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaHub.Services.Downloaders;
using MediaHub.Services.Interfaces;

namespace MediaHub.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly DownloaderFactory _factory;
    private readonly IFolderPickerService _folderPicker;
    private readonly IDialogService _dialog;

    private readonly object _ctsLock = new();
    private CancellationTokenSource? _cts;
    private IDownloader? _currentDownloader;
    private string _resolvedDomain = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPlatformBadge))]
    [NotifyPropertyChangedFor(nameof(CanStartDownload))]
    [NotifyPropertyChangedFor(nameof(PlatformKey))]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    private string _url = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutputPathDisplay))]
    private string _outputPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

    /// <summary>
    /// Shows a placeholder when no folder was picked yet.
    /// </summary>
    public string OutputPathDisplay =>
        string.IsNullOrWhiteSpace(OutputPath) ? "No folder selected" : OutputPath;

    [ObservableProperty]
    private string _platformName = "Auto-detect";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPlatformBadge))]
    [NotifyPropertyChangedFor(nameof(CanStartDownload))]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    private bool _isDownloading;

    /// <summary>
    /// Platform badge is only relevant while picking a link,
    /// it disappears once the download is running.
    /// </summary>
    public bool ShowPlatformBadge =>
        !string.IsNullOrWhiteSpace(Url) && !IsDownloading && PlatformKey != "unknown";

    /// <summary>
    /// Mirror of CanStartDownloadCore for the visual state of the download button.
    /// </summary>
    public bool CanStartDownload => CanStartDownloadCore();

    private bool CanStartDownloadCore() =>
        !IsDownloading && !string.IsNullOrWhiteSpace(Url) && _currentDownloader is not null;

    /// <summary>
    /// Stable key for styling the platform chip and input icon:
    /// "tiktok", "youtube", "soundcloud" or "unknown".
    /// </summary>
    public string PlatformKey
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Url))
                return "unknown";
            if (UrlHelpers.UrlBelongsTo(Url, "tiktok.com", "vm.tiktok.com"))
                return "tiktok";
            if (UrlHelpers.UrlBelongsTo(Url, "youtube.com", "youtu.be"))
                return "youtube";
            if (UrlHelpers.UrlBelongsTo(Url, "soundcloud.com"))
                return "soundcloud";
            return "unknown";
        }
    }

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _progressText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowStatus))]
    private string _status = string.Empty;

    [ObservableProperty]
    private string _statusKind = string.Empty;

    public bool ShowStatus => !string.IsNullOrEmpty(Status);

    [ObservableProperty]
    private int _themeIndex;

    public string VersionText => $"MediaHub v{AppInfo.Current.VersionString}  |  daniel-skliphosovsky";

    public MainViewModel(DownloaderFactory factory, IFolderPickerService folderPicker, IDialogService dialog)
    {
        _factory = factory;
        _folderPicker = folderPicker;
        _dialog = dialog;

        // Sync initial theme
        ThemeIndex = Application.Current?.UserAppTheme == AppTheme.Dark ? 1 : 0;
    }

    partial void OnUrlChanged(string value)
    {
        // Re-resolve the downloader only when the domain actually changes,
        // not on every keystroke; everything here is cheap.
        var domain = UrlHelpers.GetDomain(value);
        if (string.Equals(domain, _resolvedDomain, StringComparison.Ordinal) && domain.Length > 0)
        {
            OnPropertyChanged(nameof(PlatformKey));
            return;
        }

        _resolvedDomain = domain;
        _currentDownloader = _factory.GetDownloader(value);
        PlatformName = _currentDownloader?.PlatformName
            ?? (string.IsNullOrWhiteSpace(value) ? "Auto-detect" : "Unknown");

        OnPropertyChanged(nameof(PlatformKey));
    }

    [RelayCommand]
    private async Task PickFolderAsync()
    {
        var path = await _folderPicker.PickFolderAsync();
        if (!string.IsNullOrEmpty(path))
            OutputPath = path;
    }

    [RelayCommand(CanExecute = nameof(CanStartDownloadCore))]
    private async Task DownloadAsync()
    {
        if (string.IsNullOrWhiteSpace(OutputPath))
        {
            await _dialog.ShowErrorAsync("Please select a folder first.", "No folder selected");
            return;
        }

        var downloader = _currentDownloader!;
        var cts = new CancellationTokenSource();

        lock (_ctsLock)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = cts;
        }

        IsDownloading = true;
        Progress = 0;
        ProgressText = "Starting...";
        Status = string.Empty;
        StatusKind = string.Empty;

        try
        {
            // Progress<T> is created on the UI thread, so its callbacks are
            // marshalled back to the UI context automatically.
            var progress = new Progress<DownloadProgress>(p =>
            {
                Progress = p.Percentage ?? 0;
                ProgressText = FormatProgress(p);
                if (!string.IsNullOrWhiteSpace(p.Status))
                    Status = p.Status;
            });

            var result = await downloader.DownloadAsync(Url, OutputPath, progress, cts.Token);

            if (result.Success)
            {
                Status = "Downloaded successfully";
                StatusKind = "success";
                await _dialog.ShowAlertAsync("Success", $"Saved to {result.FilePath}");
            }
            else if (cts.IsCancellationRequested ||
                     string.Equals(result.ErrorMessage, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                Status = "Download cancelled";
                StatusKind = "muted";
            }
            else
            {
                Status = $"Failed: {result.ErrorMessage}";
                StatusKind = "error";
                await _dialog.ShowErrorAsync(result.ErrorMessage ?? "Something went wrong while downloading");
            }
        }
        catch (OperationCanceledException)
        {
            Status = "Download cancelled";
            StatusKind = "muted";
        }
        catch (Exception ex)
        {
            Status = $"Failed: {ex.Message}";
            StatusKind = "error";
            await _dialog.ShowErrorAsync(ex.Message);
        }
        finally
        {
            IsDownloading = false;
            ProgressText = string.Empty;

            lock (_ctsLock)
            {
                if (ReferenceEquals(_cts, cts))
                {
                    _cts.Dispose();
                    _cts = null;
                }
            }
        }
    }

    private static string FormatProgress(DownloadProgress p)
    {
        var parts = new List<string>(3);

        if (p.TotalBytes is > 0)
            parts.Add($"{FormatBytes(p.BytesReceived)} of {FormatBytes(p.TotalBytes.Value)}");

        if (p.Percentage is { } percent)
            parts.Add($"{percent * 100:F0}%");

        return parts.Count > 0 ? string.Join("  ", parts) : string.Empty;
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return unit == 0
            ? $"{bytes} {units[unit]}"
            : $"{size:0.0} {units[unit]}";
    }

    [RelayCommand]
    private void CancelDownload() => CancelPending();

    /// <summary>
    /// Cancels the running download, if any. Called by the Cancel button
    /// and from the page lifecycle when the window is hidden or closed.
    /// </summary>
    public void CancelPending()
    {
        lock (_ctsLock)
            _cts?.Cancel();
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        if (Application.Current is null)
            return;

        Application.Current.UserAppTheme = Application.Current.UserAppTheme == AppTheme.Light
            ? AppTheme.Dark
            : AppTheme.Light;

        ThemeIndex = Application.Current.UserAppTheme == AppTheme.Dark ? 1 : 0;
    }
}
