# MediaHub

![CI](https://img.shields.io/github/actions/workflow/status/daniel-skliphosovsky/MediaHub/ci.yml?style=for-the-badge&label=CI&logo=github)
[![Release](https://img.shields.io/github/actions/workflow/status/daniel-skliphosovsky/MediaHub/release.yml?style=for-the-badge&label=Release&logo=github)](https://github.com/daniel-skliphosovsky/MediaHub/releases)
[![License](https://img.shields.io/badge/License-MIT-6C5CE7?style=for-the-badge)](https://github.com/daniel-skliphosovsky/MediaHub/blob/main/LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows%2FmacOS-0078D6?style=for-the-badge)](https://github.com/daniel-skliphosovsky/MediaHub/releases)

MediaHub downloads media from popular platforms. Paste a link from TikTok, YouTube, SoundCloud, VK, or OK.ru, select a folder, and MediaHub handles the rest — it detects the platform automatically, downloads the media, and reports progress with cancel support.

## Platforms

- TikTok videos and image galleries
- YouTube videos (best available quality)
- SoundCloud tracks
- VK videos
- OK.ru videos

## Features

- Automatic platform detection from URL (short links, www./m. prefixes, any case)
- Light and dark theme with one-click switching
- Platform badge that updates live while you type the link
- Progress bar with percentage and byte counts, cancel support
- Link preview
- Responsive resizable window (opens at 900x620, up to 1160x760)
- Material Design 3 interface, app icon and splash screen
- Playlists saved in subfolders
- Localization: Russian and English
- Works without authorization

## Screenshots

![Main window](docs/screenshots/main.png)

<!-- 
Required screenshots:
- docs/screenshots/main.png — main window with preview and download controls
- docs/screenshots/downloading.png — download progress with cancel option
- docs/screenshots/playlist.png — playlist saved in subfolder
- docs/screenshots/dark_theme.png — dark theme interface
- docs/screenshots/localization.png — language switcher (RU/EN)
-->

## Installation

### Releases

Download the latest version from GitHub releases:

[![Latest Release](https://img.shields.io/badge/Download_Latest_Release-0066CC?style=for-the-badge&logo=github)](https://github.com/daniel-skliphosovsky/MediaHub/releases)

Find the most recent version at the top of the releases page and follow platform-specific instructions for Windows (.exe) and macOS (.pkg).

### Build from Source

```bash
git clone https://github.com/daniel-skliphosovsky/MediaHub.git
cd MediaHub
dotnet restore src/MediaHub.csproj
dotnet build src/MediaHub.csproj --configuration Release
```

**Requirements:**
- .NET SDK 9.0
- Windows 10+ or macOS 11+

## Tech Stack

- .NET MAUI
- CommunityToolkit.Mvvm
- CommunityToolkit.Maui
- YoutubeExplode
- SoundCloudExplode
- Microsoft.Extensions.Logging.Debug
- Microsoft.Maui.Controls
- TikTokExplode (self-contained reference library)

## Links

- [Releases](https://github.com/daniel-skliphosovsky/MediaHub/releases)
- [Contributing](CONTRIBUTING.md)

## Support

For bug reports and feature requests, use the GitHub issue tracker.