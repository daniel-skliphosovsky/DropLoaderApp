using System.Collections.ObjectModel;
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
    [NotifyPropertyChangedFor(nameof(ShowProgressText))]
    private string _progressText = string.Empty;

    /// <summary>
    /// Byte counter line ("12.3 MB of 45.6 MB"); hidden when the downloader
    /// reports no byte counts.
    /// </summary>
    public bool ShowProgressText => !string.IsNullOrEmpty(ProgressText);

    /// <summary>
    /// True while the downloader reports no percentage (unknown total size),
    /// in which case the popup shows a spinner instead of a fake percentage.
    /// </summary>
    [ObservableProperty]
    private bool _isIndeterminate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowDownloadSpeed))]
    private string _downloadSpeedText = string.Empty;

    public bool ShowDownloadSpeed => !string.IsNullOrEmpty(DownloadSpeedText);

    /// <summary>
    /// Bottom activity log: recent download events with their platform and
    /// status. Cleared whenever a new download starts.
    /// </summary>
    public ObservableCollection<LogEntry> Logs { get; } = [];

    public bool HasLogs => Logs.Count > 0;

    /// <summary>
    /// Name of the file being downloaded, shown as the title of the progress
    /// popup. Filled when the download starts from the preview title, a URL
    /// segment or the platform name.
    /// </summary>
    [ObservableProperty]
    private string _downloadFileName = string.Empty;

    /// <summary>
    /// Heading of the progress popup: "Downloading" for a single item,
    /// "Track X of Y" while a playlist is being downloaded.
    /// </summary>
    [ObservableProperty]
    private string _downloadHeadingText = "Downloading";

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
    [NotifyPropertyChangedFor(nameof(PreviewDescription))]
    [NotifyPropertyChangedFor(nameof(HasPreviewDescription))]
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
    public string PreviewDescription => Preview?.Description ?? string.Empty;
    public bool HasPreviewDescription => !string.IsNullOrEmpty(PreviewDescription);
    public string PreviewQuality => Preview?.QualityText ?? string.Empty;
    public bool HasPreviewQuality => !string.IsNullOrEmpty(PreviewQuality);
    public string PreviewDurationText => Preview?.DurationText ?? string.Empty;
    public bool HasPreviewDuration => !string.IsNullOrEmpty(PreviewDurationText);

    /// <summary>
    /// Rows for the expandable "Information" section of the preview card,
    /// fetched on demand from the platform library/API.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPreviewDetails))]
    private IReadOnlyList<ResourceDetail> _previewDetails = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPreviewDetails))]
    private bool _isDetailsVisible;

    public bool ShowPreviewDetails => IsDetailsVisible && PreviewDetails.Count > 0;

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
        PreviewDetails = [];
        IsDetailsVisible = false;

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
        IsIndeterminate = false;
        DownloadSpeedText = string.Empty;
        _lastBytes = 0;
        _lastSpeedAt = default;
        DownloadHeadingText = "Downloading";
        DownloadFileName = DeriveDownloadFileName();

        // A fresh download starts a fresh activity log: old statuses (and
        // their failures) make way for the new one.
        Logs.Clear();
        OnPropertyChanged(nameof(HasLogs));
        AddLog($"Downloading: {DownloadFileName}", "muted", PlatformKey);

        // The progress lives in a separate modal popup bound to this same
        // view model; it is closed when the download finishes, fails or is
        // cancelled (see the finally block below).
        var popup = new DownloadingPopup(this);
        try
        {
            Shell.Current.ShowPopup(popup);
        }
        catch
        {
            // Shell may be unavailable (e.g. during shutdown); the download
            // still proceeds without the progress popup.
        }

        try
        {
            // Progress<T> is created on the UI thread, so its callbacks are
            // marshalled back to the UI context automatically.
            var progress = new Progress<DownloadProgress>(p =>
            {
                if (p.Percentage is { } percent)
                {
                    Progress = percent;
                    IsIndeterminate = false;
                }
                else
                {
                    // No percentage means the total size is unknown; a bar
                    // stuck at 0% is worse than an honest spinner.
                    IsIndeterminate = true;
                }

                ProgressText = FormatProgress(p);
                UpdateSpeed(p);
            });

            // Playlist URLs expand into one target per item, each downloaded
            // in sequence through the shared single-video download path.
            var targets = new List<(string Title, string Url)>();
            if (downloader.IsPlaylistUrl(Url))
            {
                var items = await downloader.GetPlaylistItemsAsync(Url, cts.Token);
                targets.AddRange(items.Select(i => (i.Title, i.Url)));
            }
            else
            {
                targets.Add((DownloadFileName, Url));
            }

            if (targets.Count == 0)
            {
                AddLog("Failed: no items found in playlist", "error", PlatformKey);
                await _dialog.ShowErrorAsync("Couldn't find any items in this playlist.");
            }
            else
            {
                for (var i = 0; i < targets.Count; i++)
                {
                    cts.Token.ThrowIfCancellationRequested();

                    if (targets.Count > 1)
                    {
                        DownloadHeadingText = $"Track {i + 1} of {targets.Count}";
                        DownloadFileName = targets[i].Title;
                    }

                    var result = await downloader.DownloadAsync(targets[i].Url, OutputPath, progress, cts.Token);

                    if (result.Success)
                    {
                        var savedName = Path.GetFileName(result.FilePath?.TrimEnd('/')) ?? DownloadFileName;
                        AddLog($"Downloaded: {savedName}", "success", PlatformKey);
                        if (i == targets.Count - 1)
                            await _dialog.ShowAlertAsync("Success", $"Saved to {result.FilePath}");
                    }
                    else if (cts.IsCancellationRequested ||
                             string.Equals(result.ErrorMessage, "Cancelled", StringComparison.OrdinalIgnoreCase))
                    {
                        AddLog("Download cancelled", "muted", PlatformKey);
                        break;
                    }
                    else
                    {
                        AddLog($"Failed: {result.ErrorMessage}", "error", PlatformKey);
                        await _dialog.ShowErrorAsync(result.ErrorMessage ?? "Something went wrong while downloading");
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            AddLog("Download cancelled", "muted", PlatformKey);
        }
        catch (Exception ex)
        {
            AddLog($"Failed: {ex.Message}", "error", PlatformKey);
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

    /// <summary>
    /// Byte counter only ("12.3 MB of 45.6 MB", or just the received bytes
    /// when the total is unknown). No percentage anywhere.
    /// </summary>
    private static string FormatProgress(DownloadProgress p)
    {
        if (p.TotalBytes is > 0)
            return $"{FormatBytes(p.BytesReceived)} of {FormatBytes(p.TotalBytes.Value)}";

        return p.BytesReceived > 0 ? FormatBytes(p.BytesReceived) : string.Empty;
    }

    private long _lastBytes;
    private DateTime _lastSpeedAt;

    /// <summary>
    /// Sliding window speed: samples the byte counter roughly every half
    /// second and shows "x/s". Skipped when a downloader reports no bytes
    /// (e.g. TikTok hands out only the fraction, not the byte count).
    /// </summary>
    private void UpdateSpeed(DownloadProgress p)
    {
        var now = DateTime.UtcNow;

        if (_lastSpeedAt == default)
        {
            _lastBytes = p.BytesReceived;
            _lastSpeedAt = now;
            return;
        }

        var elapsed = (now - _lastSpeedAt).TotalSeconds;
        if (elapsed < 0.5 || p.BytesReceived == 0 || p.BytesReceived < _lastBytes)
            return;

        DownloadSpeedText = $"{FormatBytes((long)((p.BytesReceived - _lastBytes) / elapsed))}/s";
        _lastBytes = p.BytesReceived;
        _lastSpeedAt = now;
    }

    /// <summary>
    /// Removes a single row from the activity log (the per-row X button).
    /// </summary>
    [RelayCommand]
    private void RemoveLog(LogEntry entry)
    {
        if (Logs.Remove(entry))
            OnPropertyChanged(nameof(HasLogs));
    }

    /// <summary>
    /// Appends an activity row and keeps HasLogs in sync with the collection.
    /// </summary>
    private void AddLog(string message, string kind, string platformKey)
    {
        Logs.Add(new LogEntry
        {
            Message = message,
            Kind = kind,
            PlatformKey = platformKey,
            TimeText = DateTime.Now.ToString("HH:mm:ss"),
            IconData = GetLogIconData(platformKey, kind)
        });
        OnPropertyChanged(nameof(HasLogs));
    }

    /// <summary>
    /// Compact SVG path data for the log row icon: the platform logo when the
    /// event belongs to one, otherwise a status glyph for the kind.
    /// </summary>
    private static string GetLogIconData(string platformKey, string kind) => platformKey switch
    {
        "tiktok" => "M9 18V5l12-2v13 M9 18a3 3 0 1 1-6 0 3 3 0 0 1 6 0z M21 16a3 3 0 1 1-6 0 3 3 0 0 1 6 0z",
        "youtube" => "M22.54 6.42a2.78 2.78 0 0 0-1.94-2C18.88 4 12 4 12 4s-6.88 0-8.6.46a2.78 2.78 0 0 0-1.94 2A29 29 0 0 0 1 11.75a29 29 0 0 0 .46 5.33A2.78 2.78 0 0 0 3.4 19c1.72.46 8.6.46 8.6.46s6.88 0 8.6-.46a2.78 2.78 0 0 0 1.94-2 29 29 0 0 0 .46-5.25 29 29 0 0 0-.46-5.33z",
        "soundcloud" => "M2 13v-2 M5 13V9 M8 13V6 M11 13V8 M14 13v-3",
        "vk" => "M4 6a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2z M8.5 8.5L11 15.5l3.5-7",
        _ => kind switch
        {
            "success" => "M20 6L9 17l-5-5",
            "error" => "M12 9v4 M12 17h.01 M12 3l9 16a1 1 0 0 1-.87 1.5H3.87a1 1 0 0 1-.87-1.5z",
            // Compact cancel: a small X inside a circle.
            _ => "M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20z M15 9l-6 6 M9 9l6 6"
        }
    };

    /// <summary>
    /// Loads (once) and toggles the expandable "Information" section of the
    /// preview card. The extra metadata comes from the platform library/API
    /// and varies per platform.
    /// </summary>
    [RelayCommand]
    private async Task ToggleDetailsAsync()
    {
        var downloader = _currentDownloader;
        if (Preview is null || downloader is null || string.IsNullOrWhiteSpace(Url))
            return;

        if (PreviewDetails.Count == 0)
        {
            try
            {
                PreviewDetails = await downloader.GetDetailsAsync(Url);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                AddLog($"Failed to load details: {ex.Message}", "error", PlatformKey);
                return;
            }

            if (PreviewDetails.Count == 0)
            {
                AddLog("No additional information available for this resource", "muted", PlatformKey);
                return;
            }
        }

        IsDetailsVisible = !IsDetailsVisible;
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
