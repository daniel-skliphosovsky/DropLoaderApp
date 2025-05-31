using TikTokExplode.Publications;

namespace DropLoaderApp.Downloaders
{
    public partial class DownloadHandler
    {
        private async Task SmartDownloadFromTikTok(CancellationToken cancellationToken)
        {
            switch (await PublicationClient.GetPublicationType(DownloadLink))
            {
                case PublicationClient.PublicationType.Images:
                    await DownloadImagesFromTikTok(cancellationToken);
                    break;
                case PublicationClient.PublicationType.Video:
                    await DownloadVideoFromTikTok(cancellationToken);
                    break;
                case PublicationClient.PublicationType.Unknown:
                default:
                    DownloadingError("Incorrect URL");
                    return;
            }
        }
    }
}
