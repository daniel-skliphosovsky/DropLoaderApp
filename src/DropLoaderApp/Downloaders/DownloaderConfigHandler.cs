using CryptographerLib;
using DownloadersConfig;

namespace DropLoaderApp.Downloaders
{
	public class DownloaderConfigHandler
	{
        public static string GetSoundcloudClientId()
        {
            return Cryptographer.Decrypt(DownloadingSettings.SoundcloudClientId);
        }
	}
}

