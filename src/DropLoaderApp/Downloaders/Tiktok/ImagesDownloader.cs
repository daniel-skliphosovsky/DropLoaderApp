using TikTokExplode;
using TikTokExplode.Publications.Images;

namespace DropLoaderApp.Downloaders
{
    public partial class DownloadHandler
    {
        private async Task DownloadImagesFromTikTok(CancellationToken cancellationToken)
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

                await TikTok.DownloadImagesAsync(images, DownloadPath, progress: progress, cancellationToken: cancellationToken);
                
                DownloadingFinished();
            }
            catch (OperationCanceledException)
            {
                DownloadingCanceled();
            }
            catch (Exception ex) when (ex.Message.Contains("timed out"))
            {
                DownloadingCanceled();
            }
            catch (Exception ex)
            {
                DownloadingError(ex.Message);
            }
        }
    }
}
