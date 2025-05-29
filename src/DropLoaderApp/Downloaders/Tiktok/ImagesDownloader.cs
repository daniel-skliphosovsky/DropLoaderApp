using TikTokExplode;
using TikTokExplode.Publications.Images;

namespace DropLoaderApp.Downloaders
{
    public partial class DownloadHandler
    {
        private async Task DownloadImagesFromTikTok()
        {
            try
            {
                DownloadingStarted();

                TikTokClient TikTok = new TikTokClient();

                Progress<double> progress = new Progress<double>(procent =>
                {
                    if (popup.BindingContext is ViewModels.DownloadingProgressViewModel downloadingViewModel)
                    {
                        downloadingViewModel.DownloadingProgress = (float)procent;
                    }
                });

                List<TikTokExplode.Publications.Images.Image> images = await TikTok.Publications.Images.GetAsync(DownloadLink);

                await TikTok.DownloadImagesAsync(images, DownloadPath, progress: progress);
                
                DownloadingFinished();
            }
            catch (Exception exception)
            {
                DownloadingError(exception.Message);
            }
        }
    }
}
