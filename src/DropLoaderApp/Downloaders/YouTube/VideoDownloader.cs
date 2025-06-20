using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace DropLoaderApp.Downloaders
{
	public partial class DownloadHandler
    {
        private async Task DownloadVideoFromYoutube(CancellationToken cancellationToken)
        {
            try
            { 
                DownloadingStarted();

                YoutubeClient youtube = new YoutubeClient();

                Video video = await youtube.Videos.GetAsync(DownloadLink);

                if (video is null)
                    throw new Exception("Video not found");

                Progress<double> progress = new Progress<double>(procent =>
                {
                    if (popup.BindingContext is ViewModels.DownloadingProgressViewModel downloadingViewModel)
                    {
                        downloadingViewModel.DownloadingProgress = (float)procent;
                    }
                });

                string output = "";

                StreamManifest streamManifest = await youtube.Videos.Streams.GetManifestAsync(DownloadLink, cancellationToken);

                IVideoStreamInfo streamMuxed = streamManifest
                    .GetMuxedStreams()
                    .TryGetWithHighestVideoQuality();

                if (!(streamMuxed is null))
                {
                    AddDownloadingFileName(video.Title + " (video & audio tracks)");
                    await youtube.Videos.Streams.DownloadAsync(streamMuxed, Path.Combine(DownloadPath, MakeSafeFiletitle(video.Title) + $".{streamMuxed.Container.Name}"), progress, cancellationToken);
                }
                else
                {
                    IVideoStreamInfo? streamVideo = streamManifest
                        .GetVideoStreams()
                        .TryGetWithHighestVideoQuality();

                    if (streamVideo is null)
                    { output += "Video track not found"; }
                    else
                    {
                        AddDownloadingFileName(video.Title + " (video track)\n");
                        await youtube.Videos.Streams.DownloadAsync(streamVideo, Path.Combine(DownloadPath, MakeSafeFiletitle(video.Title) + "_video" + $".{streamVideo.Container.Name}"), progress, cancellationToken);
                    }


                    IStreamInfo? streamAudio = streamManifest
                        .GetAudioStreams()
                        .TryGetWithHighestBitrate();

                    if (streamAudio is null)
                    { output += "Audio track not found"; }
                    else
                    {
                        AddDownloadingFileName(video.Title + " (audio track)\n");
                        await youtube.Videos.Streams.DownloadAsync(streamAudio, Path.Combine(DownloadPath, MakeSafeFiletitle(video.Title) + "_audio" + $".{streamAudio.Container.Name}"), progress, cancellationToken);
                    }
                }

                DownloadingFinished(output);
            }
            catch (OperationCanceledException)
            {
                DownloadingCanceled();
            }
            catch (Exception ex) when (ex.Message.Contains("timed out"))
            {
                DownloadingCanceled();
            }
            catch (Exception ex) when (ex.Message.Contains("<Module>"))
            {
                DownloadingError("This video is not available in your country. Try use VPN or change your region in system preference");
            }
            catch (Exception ex)
            {
                DownloadingError(ex.Message);
            }
        }
    }
}

