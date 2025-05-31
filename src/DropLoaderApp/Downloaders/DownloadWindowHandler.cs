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

        private async void DownloadingFinished(string message = null)
		{
            await Shell.Current.DisplayAlert("Downloading finished", $"Successfully downloaded!\n{message}", "OK");
            await popup.CloseAsync();
		}

        private async void DownloadingError(string error)
		{
			await Shell.Current.DisplayAlert("Downloading Error", error, "OK");
            await popup.CloseAsync();
        }

        private async void DownloadingCanceled()
        {
            await Shell.Current.DisplayAlert("Downloading Canceled", "Downloading was canceled", "OK");
            await popup.CloseAsync();
        }

        private async void AddDownloadingFileName(string name)
		{
			if (popup.BindingContext is ViewModels.DownloadingProgressViewModel downloadingViewModel)
			{
                string trimmedName = name.Length > 50
                    ? name.Substring(0, 47) + "..."
                    : name;
                downloadingViewModel.DownloadingFileName = trimmedName;
			}
		}
    }
}

