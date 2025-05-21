namespace DropLoaderApp.Downloaders
{
    public partial class DownloadHandler
    {
        private async Task SmartDownload()
        {
            if (DownloadLink.ToLower().Contains("/sets/"))
            {
                await DownloaPlaylistFromSoundcloud();
            }
            else
            {
                await DownloadTrackFromSoundcloud();
            }
        }
    }
}

