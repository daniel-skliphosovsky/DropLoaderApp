using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;
using MediaHub.Models;
using MediaHub.Services.Interfaces;
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

    private bool _isLoading = true;
    private string _statusMessage = string.Empty;

    public InfoPopup(IDownloader downloader, string url)
    {
        InitializeComponent();
        _downloader = downloader;
        _url = url;

        CloseCommand = new RelayCommand(() => Close());
        BindingContext = this;

        _ = LoadDetailsAsync();
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

    private async Task LoadDetailsAsync()
    {
        try
        {
            var details = await _downloader.GetDetailsAsync(_url);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                foreach (var detail in details)
                    Details.Add(detail);

                if (Details.Count == 0)
                    StatusMessage = "No additional information available for this resource";

                IsLoading = false;
                OnPropertyChanged(nameof(HasDetails));
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                StatusMessage = $"Could not load information: {ex.Message}";
                IsLoading = false;
            });
        }
    }

    private void OnCloseTapped(object? sender, TappedEventArgs e) => Close();
}
