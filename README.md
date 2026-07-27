<h1 align="center">DropLoader</h1>
<p align="center">Desktop application for downloading media from TikTok, YouTube, and SoundCloud. Built with .NET MAUI.</p>
<p align="center">
  <a href="https://github.com/daniel-skliphosovsky/DropLoaderApp/actions/workflows/ci.yml"><img src="https://github.com/daniel-skliphosovsky/DropLoaderApp/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
  <a href="https://github.com/daniel-skliphosovsky/DropLoaderApp/releases"><img src="https://img.shields.io/github/v/release/daniel-skliphosovsky/DropLoaderApp" alt="Release"></a>
  <a href="https://github.com/daniel-skliphosovsky/DropLoaderApp/blob/main/LICENSE"><img src="https://img.shields.io/github/license/daniel-skliphosovsky/DropLoaderApp" alt="MIT License"></a>
</p>

## Features

- Download TikTok videos and image galleries
- Download YouTube videos (best available quality)
- Download SoundCloud tracks
- Light and Dark theme with automatic switching
- Progress tracking with cancel support
- Auto-detect platform from URL
- Material Design 3 UI
- Fixed window size (680x480)

## Requirements

- .NET 10.0 SDK
- Windows 10 (version 19041) or later
- macOS 11 (Big Sur) or later

## Installation

### Windows

Download `DropLoaderApp.App.exe` from the latest Release and run it.

### macOS

Download `DropLoader-macOS.zip` from the latest Release, extract, and move the app to Applications.

## Usage

1. Paste a URL from TikTok, YouTube, or SoundCloud
2. Select output folder
3. Click Download
4. Track progress and cancel if needed

### Screenshots

<table>
  <tr>
    <td>MacOS Light</td>
    <td>MacOS Dark</td>
  </tr>
  <tr>
    <td><img src="images/MacOS/DownloadingPage_LightTheme.png" alt="MacOS Light" width="300"></td>
    <td><img src="images/MacOS/DownloadingPage_DarkTheme.png" alt="MacOS Dark" width="300"></td>
  </tr>
  <tr>
    <td>Windows Light</td>
    <td>Windows Dark</td>
  </tr>
  <tr>
    <td><img src="images/Windows/DownloadingPage_LightTheme.jpg" alt="Windows Light" width="300"></td>
    <td><img src="images/Windows/DownloadingPage_DarkTheme.jpg" alt="Windows Dark" width="300"></td>
  </tr>
</table>

## Platform Support

| Platform | URL Examples | Supported |
|----------|--------------|-----------|
| TikTok | `tiktok.com/@user/video/123`, `vm.tiktok.com/ABC` | Yes |
| YouTube | `youtube.com/watch?v=123`, `youtu.be/123` | Yes |
| SoundCloud | `soundcloud.com/user/track`, `soundcloud.com/user/sets/playlist` | Yes |

## Possible Problems

- **TikTok**: Rate limiting may cause empty responses. Wait a few minutes and try again.
- **YouTube**: YouTube may change their streaming policies. Videos may not always be available.
- **SoundCloud**: Some tracks may have restricted access or require authentication.
- **Download path**: Ensure the output folder exists and is writable.

## Build from Source

```bash
git clone https://github.com/daniel-skliphosovsky/DropLoaderApp.git
cd DropLoaderApp
dotnet restore
dotnet build tests/DropLoaderApp.Tests/DropLoaderApp.Tests.csproj --configuration Release
dotnet test tests/DropLoaderApp.Tests/DropLoaderApp.Tests.csproj --configuration Release
```

## Dependencies

- TikTokExplode (custom library, Clean Architecture)
- YoutubeExplode 6.5.4
- SoundCloudExplode 1.6.5
- CommunityToolkit.Mvvm
- CommunityToolkit.Maui

## License

This project is licensed under the MIT License.
