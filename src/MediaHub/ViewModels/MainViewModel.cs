using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Views;
using MediaHub.Models;
using MediaHub.Services.Downloaders;
using MediaHub.Services.Interfaces;
using MediaHub.Views;
using Microsoft.Maui.ApplicationModel;

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

    private readonly object _previewLock = new();
    private CancellationTokenSource? _previewCts;

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
    /// "tiktok", "youtube", "soundcloud", "vk" or "unknown".
    /// </summary>
    public string PlatformKey
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Url))
                return "unknown";
            if (UrlHelpers.UrlBelongsTo(Url, "tiktok.com", "vm.tiktok.com", "vt.tiktok.com", "www.tiktok.com", "m.tiktok.com"))
                return "tiktok";
            if (UrlHelpers.UrlBelongsTo(Url, "youtube.com", "youtu.be"))
                return "youtube";
            if (UrlHelpers.UrlBelongsTo(Url, "soundcloud.com"))
                return "soundcloud";
            if (UrlHelpers.UrlBelongsTo(Url, "vk.com", "m.vk.com", "vkvideo.ru", "m.vkvideo.ru"))
                return "vk";
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

    /// <summary>
    /// Name of the file being downloaded, shown as the title of the progress
    /// popup. Filled when the download starts from the preview title, a URL
    /// segment or the platform name.
    /// </summary>
    [ObservableProperty]
    private string _downloadFileName = string.Empty;

    public bool ShowStatus => !string.IsNullOrEmpty(Status);

    [ObservableProperty]
    private int _themeIndex;

    /// <summary>
    /// Metadata for the "what will be downloaded" card, filled after the URL
    /// settles and the platform is known.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreview))]
    [NotifyPropertyChangedFor(nameof(ShowPreviewSection))]
    [NotifyPropertyChangedFor(nameof(PreviewTitle))]
    [NotifyPropertyChangedFor(nameof(PreviewAuthor))]
    [NotifyPropertyChangedFor(nameof(HasPreviewAuthor))]
    [NotifyPropertyChangedFor(nameof(PreviewThumbnailUrl))]
    [NotifyPropertyChangedFor(nameof(HasPreviewThumbnail))]
    [NotifyPropertyChangedFor(nameof(PreviewQuality))]
    [NotifyPropertyChangedFor(nameof(PreviewDurationText))]
    [NotifyPropertyChangedFor(nameof(HasPreviewDuration))]
    private MediaPreview? _preview;

    [ObservableProperty]
    private bool _isPreviewLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPreviewError))]
    private string? _previewError;

    public bool HasPreview => Preview is not null;

    /// <summary>
    /// Keeps the card visible while the preview is being fetched, so the
    /// spinner has somewhere to live.
    /// </summary>
    public bool ShowPreviewSection => HasPreview || IsPreviewLoading;

    public bool ShowPreviewError => !string.IsNullOrEmpty(PreviewError);

    public string PreviewTitle => Preview?.Title ?? string.Empty;
    public string PreviewAuthor => Preview?.Author ?? string.Empty;
    public bool HasPreviewAuthor => !string.IsNullOrEmpty(PreviewAuthor);
    public string PreviewThumbnailUrl => Preview?.ThumbnailUrl ?? string.Empty;
    public bool HasPreviewThumbnail => !string.IsNullOrEmpty(PreviewThumbnailUrl);
    public string PreviewQuality => Preview?.QualityText ?? string.Empty;
    public bool HasPreviewQuality => !string.IsNullOrEmpty(PreviewQuality);
    public string PreviewDurationText => Preview?.DurationText ?? string.Empty;
    public bool HasPreviewDuration => !string.IsNullOrEmpty(PreviewDurationText);

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
        }
        else
        {
            _resolvedDomain = domain;
            _currentDownloader = _factory.GetDownloader(value);
            PlatformName = _currentDownloader?.PlatformName
                ?? (string.IsNullOrWhiteSpace(value) ? "Auto-detect" : "Unknown");

            OnPropertyChanged(nameof(PlatformKey));
        }

        SchedulePreview();
    }

    /// <summary>
    /// Debounced preview fetch: cancels any pending request, hides the stale
    /// card, then loads fresh metadata after the user stops typing.
    /// </summary>
    private void SchedulePreview()
    {
        lock (_previewLock)
        {
            _previewCts?.Cancel();
            _previewCts?.Dispose();
            _previewCts = new CancellationTokenSource();
        }

        Preview = null;
        PreviewError = null;
        IsPreviewLoading = false;

        var downloader = _currentDownloader;
        if (downloader is null || string.IsNullOrWhiteSpace(Url))
            return;

        var cts = _previewCts;
        _ = LoadPreviewAfterDebounceAsync(Url, downloader, cts);
    }

    private async Task LoadPreviewAfterDebounceAsync(string url, IDownloader downloader, CancellationTokenSource cts)
    {
        var ct = cts.Token;
        try
        {
            await Task.Delay(650, ct);
            SetPreviewLoading(true);

            var preview = await downloader.GetPreviewAsync(url, ct);
            if (ct.IsCancellationRequested)
                return;

            SetPreviewResult(() =>
            {
                if (ct.IsCancellationRequested)
                    return;

                Preview = preview;
                PreviewError = preview is null ? "Couldn't load preview" : null;
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
            if (ct.IsCancellationRequested)
                return;

            // Preview is best-effort: never block the download because of it.
            SetPreviewResult(() =>
            {
                if (ct.IsCancellationRequested)
                    return;

                Preview = null;
                PreviewError = "Couldn't load preview";
            });
        }
        finally
        {
            // Only the current generation of the preview request may touch the
            // spinner; an older cancelled one must leave it alone.
            SetPreviewResult(() =>
            {
                lock (_previewLock)
                {
                    if (ReferenceEquals(_previewCts, cts))
                        IsPreviewLoading = false;
                }
            });
        }
    }

    /// <summary>
    /// The preview libraries may resume on a thread pool thread, so any
    /// observable state mutation is marshalled back to the UI thread.
    /// </summary>
    private static void SetPreviewResult(Action apply)
    {
        if (MainThread.IsMainThread)
            apply();
        else
            MainThread.BeginInvokeOnMainThread(apply);
    }

    private void SetPreviewLoading(bool isLoading) =>
        SetPreviewResult(() => IsPreviewLoading = isLoading);

    private void CancelPreview()
    {
        lock (_previewLock)
        {
            _previewCts?.Cancel();
            _previewCts?.Dispose();
            _previewCts = null;
        }

        // Keep an already-loaded card visible during the download, just stop
        // any pending fetch and its spinner.
        IsPreviewLoading = false;
        PreviewError = null;
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

        // A download supersedes any in-flight preview request.
        CancelPreview();

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
        DownloadFileName = DeriveDownloadFileName();

        // The progress lives in a separate modal popup bound to this same
        // view model; it is closed when the download finishes, fails or is
        // cancelled (see the finally block below).
        var popup = new DownloadingPopup(this);
        Shell.Current.ShowPopup(popup);

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

            try
            {
                await popup.CloseAsync();
            }
            catch
            {
                // The window may already be closing while the download is
                // cancelled; dismissing the popup is best-effort.
            }

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

    /// <summary>
    /// Picks the file name shown in the progress popup: the preview title when
    /// available, otherwise the last URL segment, otherwise the platform name.
    /// </summary>
    private string DeriveDownloadFileName()
    {
        if (!string.IsNullOrWhiteSpace(Preview?.Title))
            return Preview.Title;

        var segment = Url.TrimEnd('/').Split('/').LastOrDefault();
        if (!string.IsNullOrWhiteSpace(segment))
            return segment;

        return $"{PlatformName} download";
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
