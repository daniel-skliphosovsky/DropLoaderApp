namespace DropLoaderApp.Downloaders
{
	public partial class DownloadHandler
	{
		public static string? DownloadLink;

		public static string? DownloadPath;

		private enum LinkPlatform
		{
			SoundCloud,
			TikTok,
			YouTube,
			Unknown
		}

		private string MakeSafeFiletitle(string fileTitle)
		{
			return string.Join("_", fileTitle.Split(Path.GetInvalidFileNameChars()));
		}

		private LinkPlatform GetLinkPlatform()
		{
			string link = DownloadLink.ToLower();
			if (link.Contains("soundcloud.com"))
				return LinkPlatform.SoundCloud;
            if (link.Contains("tiktok.com"))
                return LinkPlatform.TikTok;
            if (link.Contains("youtube.com") || link.Contains("youtu.be"))
                return LinkPlatform.YouTube;
            else return LinkPlatform.Unknown;
		}

        public async Task TryDownload()
		{
			CancellationToken cancellationToken = default;
            if (popup.BindingContext is ViewModels.DownloadingProgressViewModel downloadingViewModel)
            {
				cancellationToken = downloadingViewModel.CancellationTokenSource.Token;
            }
            switch (GetLinkPlatform())
			{
				case LinkPlatform.SoundCloud:
					await SmartDownloadFromSoundcloud(cancellationToken);
					break;
				case LinkPlatform.TikTok:
					await SmartDownloadFromTikTok(cancellationToken);
					break;
                case LinkPlatform.YouTube:
                    await DownloadVideoFromYoutube(cancellationToken);
                    break;
                case LinkPlatform.Unknown:
				default:
					DownloadingError("Link is incorrect");
					break;
			}
        }
    }
}

