# DropLoaderApp
An application that helps you to download media from popular platforms

Built with .NET MAUI for macOS and Windows. Paste a link from TikTok, YouTube or SoundCloud, pick a folder, and DropLoader takes care of the rest — it detects the platform automatically, downloads the media, and reports progress with the option to cancel at any time.

**v2.1.0** brings a rewritten TikTok downloader on the updated TikTokExplode library, a single shared HTTP client for all downloaders, and a refreshed Material 3 interface with button states, smoother transitions and clearer status feedback.

# Screenshots

#### Downloading Page
<table>
  <tr>
    <td><img src="images/MacOS/DownloadingPage_LightTheme.png" width="300" alt="Light Theme Page"></td>
    <td><img src="images/MacOS/DownloadingPage_DarkTheme.png" width="300" alt="Dark Theme Page"></td>
  </tr>
  <tr>
    <td align="center">Light Theme</td>
    <td align="center">Dark Theme</td>
  </tr>
  <tr>
    <td><img src="images/Windows/DownloadingPage_LightTheme.jpg" width="300" alt="Light Theme Page"></td>
    <td><img src="images/Windows/DownloadingPage_DarkTheme.jpg" width="300" alt="Dark Theme Page"></td>
  </tr>
</table>

#### Downloading Process
<table>
  <tr>
    <td><img src="images/MacOS/DownloadingContext_LightTheme.png" width="300" alt="Light Theme Context"></td>
    <td><img src="images/MacOS/DownloadingContext_DarkTheme.png" width="300" alt="Dark Theme Context"></td>
  </tr>
  <tr>
    <td align="center">Light Theme</td>
    <td align="center">Dark Theme</td>
  </tr>
  <tr>
    <td><img src="images/Windows/DownloadingContext_LightTheme.jpg" width="300" alt="Light Theme Context"></td>
    <td><img src="images/Windows/DownloadingContext_DarkTheme.jpg" width="300" alt="Dark Theme Context"></td>
  </tr>
</table>

#### Possible downloading completions
<table>
  <tr>
    <td><img src="images/MacOS/DownloadingCanceled.png" width="300" alt="Downloading Canceled"></td>
    <td><img src="images/MacOS/DownloadingFinished.png" width="300" alt="Downloading Finished"></td>
  </tr>
  <tr>
    <td align="center">Downloading Canceled</td>
    <td align="center">Downloading Finished</td>
  </tr>
  <tr>
    <td><img src="images/Windows/DownloadingCanceled.jpg" width="300" alt="Downloading Canceled"></td>
    <td><img src="images/Windows/DownloadingFinished.jpg" width="300" alt="Downloading Finished"></td>
  </tr>
</table>

#### Possible Errors
<table>
  <tr>
    <td><img src="images/MacOS/ErrorType_EmptyFields.png" width="300" alt="Empty Fields Error"></td>
    <td><img src="images/MacOS/ErrorType_IncorrectLink.png" width="300" alt="Incorrect Link Error"></td>
  </tr>
  <tr>
    <td align="center">Empty Fields</td>
    <td align="center">Incorrect Links</td>
  </tr>
  <tr>
    <td><img src="images/Windows/ErrorType_EmptyFields.jpg" width="300" alt="Empty Fields Error"></td>
    <td><img src="images/Windows/ErrorType_IncorrectLink.jpg" width="300" alt="Incorrect Link Error"></td>
  </tr>
</table>

# Features
- TikTok videos and image galleries
- YouTube videos (best available quality)
- SoundCloud tracks
- Automatic platform detection from the URL (short links, www./m. prefixes, any case)
- Light and Dark theme with one-click switching
- Progress bar with percentage, cancel support
- Material Design 3 interface with button states and smooth transitions
- Fixed window size (680x480)

# Usage
1. Open App
2. Paste the media link into the «Link» field (TikTok, YouTube or SoundCloud)
3. Select the path where the file will be saved
4. Click the «Download» Button to start downloading
5. Track the progress, cancel anytime if needed

# Possible Problems

**Soundcloud**: Some tracks may be unavailable and you may get a "This track is not available" error -> Solution: try to find another link to this track

**TikTok**: If the publication you provided a link to is private (or does not exist) then the program will download another random video (This is related to TikTok API). Therefore, sometimes after downloading you may find a completely different video / photo.
Also, don't be alarmed if the download doesn't start. This is also related to TikTok API -> Solution: just wait a bit (usually up to 10 seconds) and the download will start

**YouTube**: Due to changes in YouTube policy, audio track can be in .webm extension

# Install

### Getting Started

1. **Go to Releases**
   Download the latest version from our GitHub releases page:

   [![Latest Release](https://img.shields.io/badge/Download_Latest_Release-0066CC?style=for-the-badge&logo=github)](https://github.com/daniel-skliphosovsky/DropLoaderApp/releases)

2. **Find the latest release**
   Look for the most recent version at the top of the releases page

3. **Follow platform-specific instructions**
   Complete installation guides for both MacOS and Windows are available in the release description

### Platform Support

| Platform | Installation Method |
|----------|---------------------|
| Windows  | `.exe` (standard installer) |
| MacOS    | `.pkg` (macOS installer package) |

# Build from Source

```bash
git clone https://github.com/daniel-skliphosovsky/DropLoaderApp.git
cd DropLoaderApp
dotnet restore src/DropLoaderApp.App/DropLoaderApp.App.csproj
dotnet build src/DropLoaderApp.App/DropLoaderApp.App.csproj --configuration Release
```

# License

This project is licensed under the MIT License.
