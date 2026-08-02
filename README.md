# MediaHub

An application that helps you to download media from popular platforms.

Built with .NET MAUI for macOS and Windows. Paste a link from TikTok, YouTube, SoundCloud, VK or OK.ru, pick a folder, and MediaHub takes care of the rest — it detects the platform automatically, downloads the media, and reports progress with the option to cancel at any time.

**v4.1.0** adds a new downloader for OK.ru, a friendly SoundCloud error message and editable platform logo sources in Resources/Logos (edited as SVG by hand, the app uses the generated PNGs). The window opens at a compact size (900x620, resizable up to 1160x760), the download block floats in the center of the screen, and the whole interface was scaled up — larger fields, buttons and typography, a cleaner single-outline input, the app icon as the header logo, and a moon-only theme toggle.

[![CI](https://img.shields.io/github/actions/workflow/status/daniel-skliphosovsky/MediaHub/ci.yml?style=for-the-badge&label=CI&logo=github)](https://github.com/daniel-skliphosovsky/MediaHub/actions)
[![Release](https://img.shields.io/github/actions/workflow/status/daniel-skliphosovsky/MediaHub/release.yml?style=for-the-badge&label=Release&logo=github)](https://github.com/daniel-skliphosovsky/MediaHub/actions)
[![License](https://img.shields.io/badge/License-MIT-6C5CE7?style=for-the-badge)](https://github.com/daniel-skliphosovsky/MediaHub/blob/main/LICENSE)

# Screenshots

#### Downloading Page

| | |
|---|---|
| <img src="images/MacOS/DownloadingPage_LightTheme.png" width="300" alt="Light Theme Page"> | <img src="images/MacOS/DownloadingPage_DarkTheme.png" width="300" alt="Dark Theme Page"> |
| <p align="center">Light Theme</p> | <p align="center">Dark Theme</p> |
| <img src="images/Windows/DownloadingPage_LightTheme.jpg" width="300" alt="Light Theme Page"> | <img src="images/Windows/DownloadingPage_DarkTheme.jpg" width="300" alt="Dark Theme Page"> |

#### Downloading Process

| | |
|---|---|
| <img src="images/MacOS/DownloadingContext_LightTheme.png" width="300" alt="Light Theme Context"> | <img src="images/MacOS/DownloadingContext_DarkTheme.png" width="300" alt="Dark Theme Context"> |
| <p align="center">Light Theme</p> | <p align="center">Dark Theme</p> |
| <img src="images/Windows/DownloadingContext_LightTheme.jpg" width="300" alt="Light Theme Context"> | <img src="images/Windows/DownloadingContext_DarkTheme.jpg" width="300" alt="Dark Theme Context"> |

#### Possible downloading completions

| | |
|---|---|
| <img src="images/MacOS/DownloadingCanceled.png" width="300" alt="Downloading Canceled"> | <img src="images/MacOS/DownloadingFinished.png" width="300" alt="Downloading Finished"> |
| <p align="center">Downloading Canceled</p> | <p align="center">Downloading Finished</p> |
| <img src="images/Windows/DownloadingCanceled.jpg" width="300" alt="Downloading Canceled"> | <img src="images/Windows/DownloadingFinished.jpg" width="300" alt="Downloading Finished"> |

#### Possible Errors

| | |
|---|---|
| <img src="images/MacOS/ErrorType_EmptyFields.png" width="300" alt="Empty Fields Error"> | <img src="images/MacOS/ErrorType_IncorrectLink.png" width="300" alt="Incorrect Link Error"> |
| <p align="center">Empty Fields</p> | <p align="center">Incorrect Links</p> |
| <img src="images/Windows/ErrorType_EmptyFields.jpg" width="300" alt="Empty Fields Error"> | <img src="images/Windows/ErrorType_IncorrectLink.jpg" width="300" alt="Incorrect Link Error"> |

# Features

- TikTok videos and image galleries
- YouTube videos (best available quality)
- SoundCloud tracks
- VK videos
- OK.ru videos
- Automatic platform detection from the URL (short links, www./m. prefixes, any case)
- Light and Dark theme with one-click switching
- Platform badge that updates live while you type the link
- Progress bar with percentage and byte counts, cancel support
- Link preview
- Responsive resizable window (opens at 900x620, up to 1160x760)
- Material Design 3 interface, app icon and splash screen

# Usage

1. Open App
2. Paste the media link into the link field (TikTok, YouTube, SoundCloud, VK, OK.ru)
3. Select the path where the file will be saved
4. Click the Download Button to start downloading
5. Track the progress, cancel anytime if needed

# Possible Problems

**Soundcloud**: Some tracks may be unavailable and you may get a "This track is not available" error -> Solution: try to find another link to this track

**TikTok**: If the publication you provided a link to is private (or does not exist) then the program will download another random video (This is related to TikTok API). Therefore, sometimes after downloading you may find a completely different video / photo.
Also, don't be alarmed if the download doesn't start. This is also related to TikTok API -> Solution: just wait a bit (usually up to 10 seconds) and the download will start

**YouTube**: Due to changes in YouTube policy, audio track can be in .webm extension

**VK**: Only public videos can be downloaded. Private or region-restricted videos return an error, and some pages may require a retry when the first attempt fails with a network error

# Install

### Getting Started

1. **Go to Releases**
   Download the latest version from our GitHub releases page:

   [![Latest Release](https://img.shields.io/badge/Download_Latest_Release-0066CC?style=for-the-badge&logo=github)](https://github.com/daniel-skliphosovsky/MediaHub/releases)

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
git clone https://github.com/daniel-skliphosovsky/MediaHub.git
cd MediaHub
dotnet restore src/MediaHub/MediaHub.csproj
dotnet build src/MediaHub/MediaHub.csproj --configuration Release
```

# Tech Stack

- .NET MAUI
- CommunityToolkit.Mvvm
- CommunityToolkit.Maui
- YoutubeExplode
- SoundCloudExplode
- Microsoft.Extensions.Logging.Debug
- Microsoft.Maui.Controls
- TikTokExplode (self-contained reference library)

# Links

- [Releases](https://github.com/daniel-skliphosovsky/MediaHub/releases)
- [Contributing](CONTRIBUTING.md)

# Support

For bug reports and feature requests, please use the GitHub issue tracker.