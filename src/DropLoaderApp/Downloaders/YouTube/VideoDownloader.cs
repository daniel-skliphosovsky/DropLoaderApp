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
                    .Where(s => s.Container == Container.Mp4)
                    .TryGetWithHighestVideoQuality();

                if (!(streamMuxed is null))
                {
                    AddDownloadingFileName(video.Title + " (video & audio tracks)");
                    await youtube.Videos.Streams.DownloadAsync(streamMuxed, Path.Combine(DownloadPath, MakeSafeFiletitle(video.Title) + ".mp4"), progress, cancellationToken);
                }
                else
                {
                    IVideoStreamInfo? streamVideo = streamManifest
                        .GetVideoStreams()
                        .Where(s => s.Container == Container.Mp4)
                        .TryGetWithHighestVideoQuality();

                    if (streamVideo is null)
                    { output += "Video track not found"; }
                    else
                    {
                        AddDownloadingFileName(video.Title + " (video track)\n");
                        await youtube.Videos.Streams.DownloadAsync(streamVideo, Path.Combine(DownloadPath, MakeSafeFiletitle(video.Title) + ".mp4"), progress, cancellationToken);
                    }


                    IStreamInfo? streamAudio = streamManifest
                        .GetAudioStreams()
                        .Where(s => s.Container == Container.Mp3)
                        .TryGetWithHighestBitrate();

                    if (streamAudio is null)
                    { output += "Audio track not found"; }
                    else
                    {
                        AddDownloadingFileName(video.Title + " (audio track)\n");
                        await youtube.Videos.Streams.DownloadAsync(streamAudio, Path.Combine(DownloadPath, MakeSafeFiletitle(video.Title) + ".mp3"), progress, cancellationToken);
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

