using SoundCloudExplode;
using SoundCloudExplode.Tracks;

namespace DropLoaderApp.Downloaders
{
    public partial class DownloadHandler
    {
        private async Task DownloadTrackFromSoundcloud()
        {
            try
            {
                DownloadingStarted();

                SoundCloudClient soundCloud = new SoundCloudClient(DownloaderConfigHandler.GetSoundcloudClientId());

                Progress<double> progress = new Progress<double>(procent =>
                {
                    if (popup.BindingContext is ViewModels.DownloadingProgressViewModel downloadingViewModel)
                    {
                        downloadingViewModel.DownloadingProgress = (float)procent;
                    }
                });

                Track? track = await soundCloud.Tracks.GetAsync(DownloadLink);

                if (track == null)
                {
                   throw new Exception("Track not found");
                }

                AddDownloadingFileName(track.Title);
                await soundCloud.DownloadAsync(track, Path.Combine(DownloadPath, MakeSafeFiletitle(track.Title) + ".mp3"), progress);


                DownloadingFinished();
            }
            catch (Exception exception)
            {
                DownloadingError(exception.Message);
            }
        }
    }
}

