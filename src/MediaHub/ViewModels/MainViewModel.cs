using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Views;
using MediaHub.Models;
using MediaHub.Services.Downloaders;
using MediaHub.Services.Interfaces;
using MediaHub.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

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
    private bool _resolvedIsContent;

    private readonly object _previewLock = new();
    private CancellationTokenSource? _previewCts;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPlatformBadge))]
    [NotifyPropertyChangedFor(nameof(CanStartDownload))]
    [NotifyPropertyChangedFor(nameof(PlatformKey))]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    private string _url = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PathDisplayText))]
    [NotifyPropertyChangedFor(nameof(IsPathSelected))]
    private string _outputPath = string.Empty;

    /// <summary>
    /// True once the user picked a save folder; the save-path entry shows a
    /// muted placeholder text until then.
    /// </summary>
    public bool IsPathSelected => !string.IsNullOrWhiteSpace(OutputPath);

    /// <summary>
    /// The chosen save path, or a muted localized placeholder asking the user
    /// to pick a folder when none was selected yet.
    /// </summary>
    public string PathDisplayText =>
        IsPathSelected ? OutputPath : Loc.Get(LocKeys.SaveToPlaceholder);

    [ObservableProperty]
    private string _platformName = Loc.Get(LocKeys.StatusAutoDetect);

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
            // A bare domain (youtube.com without a video) is not yet a
            // download; keep the icon and chip "unknown" until a real
            // resource link is recognized.
            if (string.IsNullOrWhiteSpace(Url) || !UrlHelpers.LooksLikeContentUrl(Url))
                return "unknown";
            if (UrlHelpers.UrlBelongsTo(Url, "tiktok.com", "vm.tiktok.com", "vt.tiktok.com", "www.tiktok.com", "m.tiktok.com"))
                return "tiktok";
            if (UrlHelpers.UrlBelongsTo(Url, "youtube.com", "youtu.be"))
                return "youtube";
            if (UrlHelpers.UrlBelongsTo(Url, "soundcloud.com"))
                return "soundcloud";
            if (UrlHelpers.UrlBelongsTo(Url, "vk.com", "m.vk.com", "vkvideo.ru", "m.vkvideo.ru"))
                return "vk";
            if (UrlHelpers.UrlBelongsTo(Url, "ok.ru", "m.ok.ru"))
                return "ok";
            if (UrlHelpers.UrlBelongsTo(Url, "dailymotion.com", "dai.ly"))
                return "dailymotion";
            if (UrlHelpers.UrlBelongsTo(Url, "vimeo.com", "player.vimeo.com"))
                return "vimeo";
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
    /// Name of the file being downloaded, shown under the popup heading.
    /// Filled when the download starts from the preview title, a URL segment
    /// or the platform name.
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
    /// Active UI language ("ru"/"en"), switched by the header button and
    /// persisted in Preferences.
    /// </summary>
    [ObservableProperty]
    private string _languageCode = "ru";

    /// <summary>
    /// Title of the header language button: shows the language the button
    /// switches TO, so the current choice is obvious at a glance.
    /// </summary>
    public string LocLangButton =>
        LanguageCode == "ru" ? Loc.Get(LocKeys.LangCodeEn) : Loc.Get(LocKeys.LangCodeRu);

    // Static XAML-bound strings. They are get-only wrappers over Loc, so a
    // blanket change notification (see ToggleLanguage) re-reads them under
    // the new culture and every label updates in place.
    public string LocSubtitle => Loc.Get(LocKeys.AppSubtitle);
    public string LocGithubHint => Loc.Get(LocKeys.GithubHint);
    public string LocThemeHint => Loc.Get(LocKeys.ThemeToggleHint);
    public string LocLangHint => Loc.Get(LocKeys.LangHint);
    public string LocUrlLabel => Loc.Get(LocKeys.UrlLabel);
    public string LocUrlPlaceholder => Loc.Get(LocKeys.UrlPlaceholder);
    public string LocUrlHint => Loc.Get(LocKeys.UrlHint);
    public string LocDownload => Loc.Get(LocKeys.DownloadButton);
    public string LocDownloadHint => Loc.Get(LocKeys.DownloadHint);
    public string LocInformation => Loc.Get(LocKeys.InfoButton);
    public string LocInfoHint => Loc.Get(LocKeys.InfoHint);
    public string LocSaveTo => Loc.Get(LocKeys.SaveToLabel);
    public string LocSavePathHint => Loc.Get(LocKeys.SaveToPathHint);
    public string LocBrowse => Loc.Get(LocKeys.SaveToBrowse);
    public string LocBrowseHint => Loc.Get(LocKeys.SaveToBrowseHint);

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

    public string VersionText => $"MediaHub v{AppInfo.Current.VersionString}  |  daniel-skliphosovsky";

    public MainViewModel(DownloaderFactory factory, IFolderPickerService folderPicker, IDialogService dialog)
    {
        _factory = factory;
        _folderPicker = folderPicker;
        _dialog = dialog;

        // No save folder is restored at startup: the user must pick one before
        // the first download (see the guard in DownloadAsync). The last choice
        // is still remembered in Preferences so PickFolderAsync can pre-fill
        // the picker, but it is never applied automatically.

        // Sync initial theme
        ThemeIndex = Application.Current?.UserAppTheme == AppTheme.Dark ? 1 : 0;

        // Reflect the language App activated at startup (Preferences or "ru").
        LanguageCode = Loc.CurrentCode == "ru" ? "ru" : "en";
    }

    /// <summary>
    /// Preferences key holding the last user-picked save folder.
    /// </summary>
    private const string SavePathKey = "mediahub.last_save_path";

    partial void OnUrlChanged(string value)
    {
        // Re-resolve the downloader only when the domain or the "is this a
        // real media link" verdict actually changes, not on every keystroke;
        // everything here is cheap.
        var domain = UrlHelpers.GetDomain(value);
        var isContent = UrlHelpers.LooksLikeContentUrl(value);
        if (string.Equals(domain, _resolvedDomain, StringComparison.Ordinal) &&
            isContent == _resolvedIsContent &&
            domain.Length > 0)
        {
            OnPropertyChanged(nameof(PlatformKey));
        }
        else
        {
            _resolvedDomain = domain;
            _resolvedIsContent = isContent;
            _currentDownloader = isContent ? _factory.GetDownloader(value) : null;
            PlatformName = _currentDownloader?.PlatformName
                ?? (string.IsNullOrWhiteSpace(value) ? Loc.Get(LocKeys.StatusAutoDetect) : Loc.Get(LocKeys.StatusUnknown));

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
                PreviewError = preview is null ? Loc.Get(LocKeys.StatusPreviewError) : null;
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
                PreviewError = Loc.Get(LocKeys.StatusPreviewError);
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
        {
            OutputPath = path;
            Preferences.Default.Set(SavePathKey, path);
        }
    }

    [RelayCommand(CanExecute = nameof(CanStartDownloadCore))]
    private async Task DownloadAsync()
    {
        if (string.IsNullOrWhiteSpace(OutputPath))
        {
            await _dialog.ShowErrorAsync(Loc.Get(LocKeys.DialogNoFolderMessage), Loc.Get(LocKeys.DialogNoFolderTitle));
            return;
        }

        var downloader = _currentDownloader;
        if (downloader is null)
        {
            // The button is normally disabled for unsupported links, but the
            // command can still be invoked programmatically; guard instead of
            // crashing on a null downloader.
            await _dialog.ShowErrorAsync(Loc.Get(LocKeys.DialogUnsupportedMessage), Loc.Get(LocKeys.DialogUnsupportedTitle));
            return;
        }

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
        ProgressText = Loc.Get(LocKeys.StatusStarting);
        IsIndeterminate = false;
        DownloadSpeedText = string.Empty;
        _lastBytes = 0;
        _lastSpeedAt = default;
        DownloadHeadingText = Loc.Get(LocKeys.StatusDownloading);
        DownloadFileName = DeriveDownloadFileName();

        // The progress lives in a modal popup bound to this same view model.
        // It must be on screen BEFORE the download starts so the user gets
        // immediate feedback. A failure to open it is reported as a dialog
        // instead of being swallowed silently. This block lives inside the
        // try below so the finally resets IsDownloading on every path after
        // it is set to true, including a popup failure.
        DownloadingPopup? popup = null;

        // Result dialog content; shown only after the popup is closed so the
        // alert is never hidden underneath it. Null means "no dialog" (the
        // user cancelled or nothing went wrong).
        string? resultTitle = null;
        string? resultMessage = null;

        try
        {
            try
            {
                popup = new DownloadingPopup(this);
                Shell.Current.ShowPopup(popup);
            }
            catch (Exception)
            {
                await _dialog.ShowErrorAsync(Loc.Get(LocKeys.DialogPopupError), Loc.Get(LocKeys.DialogError));
                return;
            }
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
                resultTitle = LocKeys.DialogError;
                resultMessage = Loc.Get(LocKeys.DialogEmptyPlaylist);
            }
            else
            {
                var failedCount = 0;
                for (var i = 0; i < targets.Count; i++)
                {
                    cts.Token.ThrowIfCancellationRequested();

                    if (targets.Count > 1)
                    {
                        DownloadHeadingText = Loc.Get(LocKeys.StatusTrackOf, i + 1, targets.Count);
                        DownloadFileName = targets[i].Title;
                    }

                    var result = await downloader.DownloadAsync(targets[i].Url, OutputPath, progress, cts.Token);

                    if (result.Success)
                    {
                        if (i == targets.Count - 1 && failedCount == 0)
                        {
                            resultTitle = LocKeys.DialogSuccess;
                            resultMessage = Loc.Get(LocKeys.DialogSavedTo, result.FilePath);
                        }
                    }
                    else if (cts.IsCancellationRequested ||
                             string.Equals(result.ErrorMessage, Loc.Get(LocKeys.ErrCancelled), StringComparison.OrdinalIgnoreCase))
                    {
                        // The user pressed Stop; no result dialog needed.
                        break;
                    }
                    else if (targets.Count == 1)
                    {
                        resultTitle = LocKeys.DialogError;
                        resultMessage = result.ErrorMessage ?? Loc.Get(LocKeys.DialogGenericError);
                        break;
                    }
                    else
                    {
                        // A failed item (e.g. a SoundCloud track with downloads
                        // disabled) must not abort the rest of the playlist;
                        // keep going and summarize the failures at the end.
                        failedCount++;
                    }
                }

                if (targets.Count > 1 && failedCount > 0 && !cts.IsCancellationRequested && resultTitle is null)
                {
                    resultTitle = LocKeys.DialogError;
                    resultMessage = Loc.Get(LocKeys.DialogSomeFailed, failedCount, targets.Count);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            resultTitle = LocKeys.DialogError;
            resultMessage = ex.Message;
        }
        finally
        {
            IsDownloading = false;
            ProgressText = string.Empty;
            DownloadHeadingText = string.Empty;

            if (popup is not null)
            {
                try
                {
                    await popup.CloseAsync();
                }
                catch
                {
                    // The window may already be closing while the download is
                    // cancelled; dismissing the popup is best-effort.
                }
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

        if (resultTitle is not null && resultMessage is not null)
            await _dialog.ShowAlertAsync(Loc.Get(resultTitle), resultMessage);
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

        return Loc.Get(LocKeys.StatusDownloadName, PlatformName);
    }

    /// <summary>
    /// Byte counter only ("12.3 MB of 45.6 MB", or just the received bytes
    /// when the total is unknown). No percentage anywhere.
    /// </summary>
    private static string FormatProgress(DownloadProgress p)
    {
        if (p.TotalBytes is > 0)
            return Loc.Get(LocKeys.StatusOf, FormatBytes(p.BytesReceived), FormatBytes(p.TotalBytes.Value));

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

        DownloadSpeedText = Loc.Get(LocKeys.StatusPerSecond, FormatBytes((long)((p.BytesReceived - _lastBytes) / elapsed)));
        _lastBytes = p.BytesReceived;
        _lastSpeedAt = now;
    }

    /// <summary>
    /// Opens the modal "Information" popup for the current resource. The
    /// metadata request runs inside the popup (which shows a loading state
    /// until the platform library answers), not in the card.
    /// </summary>
    [RelayCommand]
    private void OpenInfo()
    {
        var downloader = _currentDownloader;
        if (downloader is null || string.IsNullOrWhiteSpace(Url))
            return;

        try
        {
            Shell.Current.ShowPopup(new InfoPopup(downloader, Url));
        }
        catch
        {
            // Shell may be unavailable (e.g. during shutdown); the popup
            // simply does not appear in that case.
        }
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

    [RelayCommand]
    private void ToggleLanguage()
    {
        LanguageCode = LanguageCode == "ru" ? "en" : "ru";
        Loc.SetLanguage(LanguageCode);
        Preferences.Default.Set(Loc.LanguagePreferenceKey, LanguageCode);

        // All Loc* bindings are get-only wrappers; the blanket change
        // notification makes the binding engine re-read every one of them
        // under the new culture, so the whole page updates in place.
        OnPropertyChanged(string.Empty);
    }
}
