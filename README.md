# MediaHub

![CI](https://img.shields.io/github/actions/workflow/status/daniel-skliphosovsky/MediaHub/ci.yml?style=for-the-badge&label=CI&logo=github)
[![Release](https://img.shields.io/github/actions/workflow/status/daniel-skliphosovsky/MediaHub/release.yml?style=for-the-badge&label=Release&logo=github)](https://github.com/daniel-skliphosovsky/MediaHub/releases)
[![License](https://img.shields.io/badge/License-MIT-6C5CE7?style=for-the-badge)](https://github.com/daniel-skliphosovsky/MediaHub/blob/main/LICENSE)

MediaHub downloads media from TikTok, YouTube, SoundCloud, VK, and OK.ru. Paste a link, select a folder, and MediaHub handles the rest — automatic platform detection, media download, and progress tracking with cancel support.

## Platforms

- TikTok videos and image galleries
- YouTube videos (best available quality)
- SoundCloud tracks, playlists, and albums
- VK videos
- OK.ru videos

## Features

- Automatic platform detection from URL (short links, www./m. prefixes, any case)
- Light and dark theme with one-click switching
- Live platform badge while typing links
- Progress bar with percentage and byte counts, cancel support
- Link preview
- Responsive resizable window (opens at 900x620, up to 1160x760)
- Material Design 3 interface
- Playlists and albums saved in subfolders
- Localization: Russian and English
- Works without authorization

## Screenshots

![Main window](images/screenshots/main.png)

<!-- 
Required screenshots:
- images/screenshots/main.png — main window with preview and download controls
- images/screenshots/downloading.png — download progress with cancel option
- images/screenshots/playlist.png — playlist saved in subfolder
- images/screenshots/dark.png — dark theme interface
- images/screenshots/lang.png — language switcher (RU/EN)
-->

## Install

### Windows

Download the latest .exe from Releases and run it directly.

### macOS

Download the .pkg from Releases. After download:

1. Double-click the .pkg file in Finder
2. macOS will warn that the developer is not trusted
3. Go to System Settings > Privacy & Security
4. Click "Open Anyway" under "Security"
5. Authenticate with your password
6. Follow installer prompts to complete installation

### Build from source

```bash
git clone https://github.com/daniel-skliphosovsky/MediaHub.git
cd MediaHub
dotnet restore src/MediaHub.sln
dotnet build src/MediaHub.sln --configuration Release
```

**Requirements:**
- .NET SDK 9.0
- Windows 10+ or macOS 11+

## Contributing

See CONTRIBUTING.md for contribution guidelines.