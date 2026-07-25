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

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private string _outputPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

    [ObservableProperty]
    private string _platformName = "Auto-detect";

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private int _selectedTabIndex;

    private CancellationTokenSource? _cts;

    public MainViewModel(DownloaderFactory factory, IFolderPickerService folderPicker, IDialogService dialog)
    {
        _factory = factory;
        _folderPicker = folderPicker;
        _dialog = dialog;

        // Sync initial theme
        SelectedTabIndex = Application.Current?.UserAppTheme == AppTheme.Dark ? 1 : 0;
    }

    partial void OnUrlChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            PlatformName = "Auto-detect";
            return;
        }
        PlatformName = _factory.GetPlatformName(value);
    }

    [RelayCommand]
    private async Task PickFolderAsync()
    {
        var path = await _folderPicker.PickFolderAsync();
        if (!string.IsNullOrEmpty(path))
            OutputPath = path;
    }

    [RelayCommand]
    private async Task DownloadAsync()
    {
        if (string.IsNullOrWhiteSpace(Url))
        {
            await _dialog.ShowAlertAsync("Error", "Enter a URL");
            return;
        }

        var downloader = _factory.GetDownloader(Url);
        if (downloader == null)
        {
            await _dialog.ShowAlertAsync("Error", "Unsupported platform");
            return;
        }

        IsDownloading = true;
        Status = "Starting...";
        Progress = 0;
        _cts = new CancellationTokenSource();

        try
        {
            var progress = new Progress<DownloadProgress>(p =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Progress = p.Percentage ?? 0;
                    Status = p.Status;
                });
            });

            var result = await downloader.DownloadAsync(Url, OutputPath, progress, _cts.Token);

            if (result.Success)
                await _dialog.ShowAlertAsync("Success", $"Saved to {result.FilePath}");
            else
                await _dialog.ShowAlertAsync("Error", result.ErrorMessage ?? "Unknown error");
        }
        catch (OperationCanceledException)
        {
            await _dialog.ShowAlertAsync("Cancelled", "Download was cancelled");
        }
        finally
        {
            IsDownloading = false;
            Status = string.Empty;
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private void CancelDownload()
    {
        _cts?.Cancel();
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        if (Application.Current != null)
        {
            Application.Current.UserAppTheme = Application.Current.UserAppTheme == AppTheme.Light
                ? AppTheme.Dark
                : AppTheme.Light;

            SelectedTabIndex = Application.Current.UserAppTheme == AppTheme.Dark ? 1 : 0;
        }
    }
}
