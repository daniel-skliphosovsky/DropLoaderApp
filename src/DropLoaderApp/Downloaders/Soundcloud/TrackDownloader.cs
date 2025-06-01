using SoundCloudExplode;
using SoundCloudExplode.Tracks;

namespace DropLoaderApp.Downloaders
{
    public partial class DownloadHandler
    {
        private async Task DownloadTrackFromSoundcloud(CancellationToken cancellationToken)
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

                if (track is null)
                {
                   throw new Exception("Track not found");
                }

                AddDownloadingFileName(track.Title);
                await soundCloud.DownloadAsync(track, Path.Combine(DownloadPath, MakeSafeFiletitle(track.Title) + ".mp3"), progress, cancellationToken: cancellationToken);


                DownloadingFinished();
            }
            catch (OperationCanceledException)
            {
                DownloadingCanceled();
            }
            catch (Exception exception) when (exception.Message.Contains("timed out"))
            {
                DownloadingCanceled();
            }
            catch (Exception exception) when (exception.Message.Contains("Forbidden"))
            {
                DownloadingError("This track is not available. Try inserting another link");
            }
            catch (Exception exception)
            {
                DownloadingError(exception.Message);
            }
        }
    }
}

