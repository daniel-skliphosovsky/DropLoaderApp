using CommunityToolkit.Maui.Views;
using DropLoaderApp.ViewModels;

namespace DropLoaderApp.Views;

public partial class DownloadingPopup : Popup
{
	public DownloadingPopup()
	{
		InitializeComponent();
		BindingContext = new DownloadingProgressViewModel();
	}
}
