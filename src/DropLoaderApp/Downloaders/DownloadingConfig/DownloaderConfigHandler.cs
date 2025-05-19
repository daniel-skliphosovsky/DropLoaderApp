using System.Text.Json;
using System.Text.Json.Serialization;
namespace DropLoaderApp.Downloaders
{
	public class DownloaderConfigHandler
	{
        private static string _jsonPath = "/Users/daniel.skliphosovsky/Downloads/DownloaderConfig/config.json";

        public static string GetSoundcloudClientId()
        {
            DownloaderConfig config = JsonSerializer.Deserialize<DownloaderConfig>(File.ReadAllText(_jsonPath));
            return Cryptographer.Decrypt(config.SoundcloudClientId);
        }
	}

    public class DownloaderConfig
    {
        [JsonPropertyName("SoundcloudClientId")]
        public string SoundcloudClientId { get; set; }
    }

}

