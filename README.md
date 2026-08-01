# DropLoader

An application that helps you to download media from popular platforms.

Built with .NET MAUI for macOS and Windows. Paste a link from TikTok, YouTube or SoundCloud, pick a folder, and DropLoader takes care of the rest — it detects the platform automatically, downloads the media, and reports progress with the option to cancel at any time.

**v3.0.0** brings a fully redesigned interface: a responsive resizable window (980x640, minimum 780x520), a new app icon and splash screen, platform badges that light up as you type, smoother progress and status feedback, plus the folder picker fix on macOS (cancelling the dialog no longer shows an error).

[![CI](https://img.shields.io/github/actions/workflow/status/daniel-skliphosovsky/DropLoader/ci.yml?style=for-the-badge&label=CI&logo=github)](https://github.com/daniel-skliphosovsky/DropLoader/actions)
[![Release](https://img.shields.io/github/actions/workflow/status/daniel-skliphosovsky/DropLoader/release.yml?style=for-the-badge&label=Release&logo=github)](https://github.com/daniel-skliphosovsky/DropLoader/actions)
[![License](https://img.shields.io/badge/License-MIT-6C5CE7?style=for-the-badge)](https://github.com/daniel-skliphosovsky/DropLoader/blob/main/LICENSE)

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
- Progress bar with percentage and byte counts, cancel support
- Platform badge that updates live while you type the link
- Responsive resizable window (980x640, minimum 780x520)
- Material Design 3 interface, app icon and splash screen

# Usage
1. Open App
2. Paste the media link into the link field (TikTok, YouTube or SoundCloud)
3. Select the path where the file will be saved
4. Click the Download Button to start downloading
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

   [![Latest Release](https://img.shields.io/badge/Download_Latest_Release-0066CC?style=for-the-badge&logo=github)](https://github.com/daniel-skliphosovsky/DropLoader/releases)

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
git clone https://github.com/daniel-skliphosovsky/DropLoader.git
cd DropLoader
dotnet restore src/DropLoader/DropLoader.csproj
dotnet build src/DropLoader/DropLoader.csproj --configuration Release
```

# License

This project is licensed under the MIT License.
