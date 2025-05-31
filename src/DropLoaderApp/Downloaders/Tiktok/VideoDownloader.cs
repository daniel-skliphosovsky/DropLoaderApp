using TikTokExplode;
using TikTokExplode.Publications.Videos;

namespace DropLoaderApp.Downloaders
{
    public partial class DownloadHandler
    {
        private async Task DownloadVideoFromTikTok(CancellationToken cancellationToken)
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

                Video video = await TikTok.Publications.Videos.GetAsync(DownloadLink);

                await TikTok.DownloadVideoAsync(video, DownloadPath, progress: progress, cancellationToken: cancellationToken);
                
                DownloadingFinished();
            }
            catch (OperationCanceledException)
            {
                DownloadingCanceled();
            }
            catch (Exception exception)
            {
                if (exception.Message.Contains("timed out"))
                {
                    DownloadingCanceled();
                    return;
                }

                DownloadingError(exception.Message);
            }
        }
    }
}
