using SoundCloudExplode;
using SoundCloudExplode.Tracks;
using SoundCloudExplode.Playlists;

namespace DropLoaderApp.Downloaders
{
    public partial class DownloadHandler
    {
        private async Task DownloaPlaylistFromSoundcloud()
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

                Playlist playlist = await soundCloud.Playlists.GetAsync(DownloadLink, true);

                if (playlist == null)
                {
                   throw new Exception("Playlist not found");
                }

                foreach (Track track in playlist.Tracks)
                {
                    try
                    {
                        AddDownloadingFileName(track.Title);
                        await soundCloud.DownloadAsync(track, Path.Combine(DownloadPath, playlist.Title, MakeSafeFiletitle(track.Title) + ".mp3"), progress);
                    }
                    catch
                    {
                        continue;
                    }
                }
                DownloadingFinished();
            }
            catch (Exception exception)
            {
                DownloadingError(exception.Message);
            }
        }
    }
}

