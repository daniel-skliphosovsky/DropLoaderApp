using SoundCloudExplode;
using SoundCloudExplode.Tracks;

namespace DropLoaderApp.Downloaders
{
	public partial class DownloadHandler
    {
        private async Task DownloadFromSoundcloud()
		{
            try
            {
                DownloadingStarted();
                SoundCloudClient soundCloud = new SoundCloudClient(DownloaderConfigHandler.GetSoundcloudClientId());
                Track? track = await soundCloud.Tracks.GetAsync(DownloadLink);
                if (track == null)
                {
                    throw new Exception("Track not found");
                }
                Progress<double> progress = new Progress<double>(procent =>
                {
                    if (popup.BindingContext is ViewModels.DownloadingProgressViewModel downloadingViewModel)
                    {
                        downloadingViewModel.DownloadingProgress = (float)procent;
                    }
                });
                await soundCloud.DownloadAsync(track, Path.Combine(DownloadPath, MakeSafeFiletitle(track.Title) + ".mp3"), progress);
                DownloadingFinished();
            }
            catch(Exception exception)
            {
                DownloadingError(exception.Message);
            }
        }
	}
}

