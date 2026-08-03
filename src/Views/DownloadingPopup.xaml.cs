using CommunityToolkit.Maui.Views;
using MediaHub.ViewModels;

namespace MediaHub.Views;

public partial class DownloadingPopup : Popup
{
    public DownloadingPopup(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
