using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DropLoaderApp.Services.Downloaders;
using DropLoaderApp.Services.Interfaces;

namespace DropLoaderApp.ViewModels;

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
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    private string _url = string.Empty;

    [ObservableProperty]
    private string _outputPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

    [ObservableProperty]
    private string _platformName = "Auto-detect";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPlatformBadge))]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    private bool _isDownloading;

    /// <summary>
    /// Platform badge is only relevant while picking a link,
    /// it disappears once the download is running.
    /// </summary>
    public bool ShowPlatformBadge => !string.IsNullOrWhiteSpace(Url) && !IsDownloading;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private int _themeIndex;

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
            return;

        _resolvedDomain = domain;
        _currentDownloader = _factory.GetDownloader(value);
        PlatformName = _currentDownloader?.PlatformName
            ?? (string.IsNullOrWhiteSpace(value) ? "Auto-detect" : "Unknown");
    }

    private bool CanDownload() =>
        !IsDownloading && !string.IsNullOrWhiteSpace(Url) && _currentDownloader is not null;

    [RelayCommand]
    private async Task PickFolderAsync()
    {
        var path = await _folderPicker.PickFolderAsync();
        if (!string.IsNullOrEmpty(path))
            OutputPath = path;
    }

    [RelayCommand(CanExecute = nameof(CanDownload))]
    private async Task DownloadAsync()
    {
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
        Status = "Starting...";

        try
        {
            // Progress<T> is created on the UI thread, so its callbacks are
            // marshalled back to the UI context automatically.
            var progress = new Progress<DownloadProgress>(p =>
            {
                Progress = p.Percentage ?? 0;
                Status = p.Status;
            });

            var result = await downloader.DownloadAsync(Url, OutputPath, progress, cts.Token);

            if (result.Success)
            {
                Status = "Done";
                await _dialog.ShowAlertAsync("Success", $"Saved to {result.FilePath}");
            }
            else if (cts.IsCancellationRequested ||
                     string.Equals(result.ErrorMessage, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                Status = "Cancelled";
                await _dialog.ShowAlertAsync("Cancelled", "The download was cancelled");
            }
            else
            {
                await _dialog.ShowErrorAsync(result.ErrorMessage ?? "Something went wrong while downloading");
            }
        }
        catch (OperationCanceledException)
        {
            Status = "Cancelled";
            await _dialog.ShowAlertAsync("Cancelled", "The download was cancelled");
        }
        catch (Exception ex)
        {
            Status = "Error";
            await _dialog.ShowErrorAsync(ex.Message);
        }
        finally
        {
            IsDownloading = false;
            Status = string.Empty;

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
