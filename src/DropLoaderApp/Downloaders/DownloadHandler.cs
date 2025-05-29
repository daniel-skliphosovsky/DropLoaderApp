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
            else return LinkPlatform.Unknown;
		}

        public async Task TryDownload()
		{
			switch (GetLinkPlatform())
			{
				case LinkPlatform.SoundCloud:
					await SmartDownloadFromSoundcloud();
					break;
				case LinkPlatform.TikTok:
					await SmartDownloadFromTikTok();
					break;
				case LinkPlatform.Unknown:
				default:
					DownloadingError("Link is incorrect");
					break;
			}
        }
    }
}

