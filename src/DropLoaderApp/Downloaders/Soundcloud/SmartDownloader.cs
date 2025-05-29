namespace DropLoaderApp.Downloaders
{
    public partial class DownloadHandler
    {
        private async Task SmartDownloadFromSoundcloud()
        {
            if (DownloadLink.ToLower().Contains("/sets/") && !DownloadLink.Contains("?in="))
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
