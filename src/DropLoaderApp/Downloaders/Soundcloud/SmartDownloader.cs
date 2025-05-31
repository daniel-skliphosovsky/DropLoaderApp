namespace DropLoaderApp.Downloaders
{
    public partial class DownloadHandler
    {
        private async Task SmartDownloadFromSoundcloud(CancellationToken cancellationToken)
        {
            if (DownloadLink.ToLower().Contains("/sets/") && !DownloadLink.Contains("?in="))
            {
                await DownloaPlaylistFromSoundcloud(cancellationToken);
            }
            else
            {
                await DownloadTrackFromSoundcloud(cancellationToken);
            }
        }
    }
}
