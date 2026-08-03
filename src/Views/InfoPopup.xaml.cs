using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;
using MediaHub.Models;
using MediaHub.Services.Interfaces;
using MediaHub.Services.Logging;
using Microsoft.Maui.ApplicationModel;

namespace MediaHub.Views;

/// <summary>
/// Modal popup with the full metadata of the current resource. The platform
/// library request runs inside the popup: it shows a loading row while
/// GetDetailsAsync answers, then the Label/Value rows (or an error/empty
/// note), and a Close button. Self-contained so the card stays simple.
/// </summary>
public partial class InfoPopup : Popup
{
    private readonly IDownloader _downloader;
    private readonly string _url;
    private readonly CancellationTokenSource _cts = new();

    private bool _isLoading = true;
    private string _statusMessage = string.Empty;

    public InfoPopup(IDownloader downloader, string url)
    {
        InitializeComponent();
        _downloader = downloader;
        _url = url;

        CloseCommand = new RelayCommand(() => Close());
        BindingContext = this;

        // Closing the popup aborts any in-flight metadata request so the
        // underlying HTTP call does not outlive the dialog.
        Closed += (_, _) => _cts.Cancel();

        _ = LoadDetailsAsync(_cts.Token);
    }

    public ObservableCollection<ResourceDetail> Details { get; } = [];

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading == value)
                return;
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public bool HasStatus => !string.IsNullOrEmpty(StatusMessage);

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value)
                return;
            _statusMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasStatus));
        }
    }

    public bool HasDetails => Details.Count > 0;

    public IRelayCommand CloseCommand { get; }

    private async Task LoadDetailsAsync(CancellationToken ct)
    {
        try
        {
            IReadOnlyList<ResourceDetail> details = await _downloader.GetDetailsAsync(_url, ct);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                foreach (var detail in details)
                    Details.Add(detail);

                if (Details.Count == 0)
                    StatusMessage = Loc.Get(LocKeys.InfoNoInfo);

                IsLoading = false;
                OnPropertyChanged(nameof(HasDetails));
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                StatusMessage = Loc.Get(LocKeys.ErrUnknown);
                IsLoading = false;
            });
        }
    }
}
