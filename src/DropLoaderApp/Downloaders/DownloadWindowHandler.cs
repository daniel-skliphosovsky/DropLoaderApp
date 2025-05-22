using CommunityToolkit.Maui.Views;

namespace DropLoaderApp.Downloaders
{
	public partial class DownloadHandler
	{
		private Popup popup = new Views.DownloadingPopup();

        private async void DownloadingStarted()
		{
			await Shell.Current.ShowPopupAsync(popup);
		}

        private async void DownloadingFinished()
		{
            await Shell.Current.DisplayAlert("Downloading finished", "Successfully downloaded!\n", "OK");
            await popup.CloseAsync();
		}

        private async void DownloadingError(string error)
		{
			await Shell.Current.DisplayAlert("Downloading Error", error, "OK");
            await popup.CloseAsync();
        }

        private async void AddDownloadingFileName(string name)
		{
			if (popup.BindingContext is ViewModels.DownloadingProgressViewModel downloadingViewModel)
			{
				downloadingViewModel.DownloadingFileName = name;
			}
		}
    }
}

