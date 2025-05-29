using TikTokExplode.Publications;

namespace DropLoaderApp.Downloaders
{
    public partial class DownloadHandler
    {
        private async Task SmartDownloadFromTikTok()
        {
            switch (await PublicationClient.GetPublicationType(DownloadLink))
            {
                case PublicationClient.PublicationType.Images:
                    await DownloadImagesFromTikTok();
                    break;
                case PublicationClient.PublicationType.Video:
                    await DownloadVideoFromTikTok();
                    break;
                case PublicationClient.PublicationType.Unknown:
                default:
                    DownloadingError("Incorrect URL");
                    return;
            }
        }
    }
}
