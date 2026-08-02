# Changelog

All notable changes to MediaHub will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [4.2.0] - 2026-08-02

### Added

- Fault tolerance with partial file cleanup on failed downloads
- Professional repository documentation (README, LICENSE, CONTRIBUTING, issue and PR templates, SECURITY, CODE_OF_CONDUCT)

### Fixed

- SVG platform icons
- OK.ru downloader metadata and error mapping
- Logging

### Removed

- Dailymotion and Vimeo downloaders

## [4.1.0] - 2025-08-02

### Added

- OK.ru downloader
- Friendly SoundCloud error message
- Platform logos as SVG

### Changed

- Window opens at compact size (900x620, resizable up to 1160x760)
- Download block floats in the center of the screen
- Interface scaled up — larger fields, buttons and typography
- Cleaner single-outline input
- App icon as the header logo
- Moon-only theme toggle

## [4.0.9] - 2025-07-25

### Fixed

- VK icon wide rounded rectangle
- VK and YouTube icons
- Enter link label
- Platform display
- Wider content

## [4.0.8] - 2025-07-15

### Fixed

- Icons
- Preview size
- VK HTML tags
- Download popup size

## [4.0.7] - 2025-06-30

### Fixed

- Reset IsDownloading on popup failure
- YouTube icon ratio

## [4.0.6] - 2025-06-20

### Fixed

- Download popup
- Required save path
- VK detection
- Remove log panel
- GitHub logo
- Info popup
- Icons
- Refactor

## [4.0.5] - 2025-05-15

### Added

- Bigger window
- Log panel
- TikTok preview metadata
- Popup and icon fixes
- Resource info

## [4.0.4] - 2025-04-10

### Fixed

- VK payload parsing per actual API response

## [4.0.3] - 2025-03-25

### Added

- Violet theme
- Playlist downloads
- Simplified popup and preview

## [4.0.2] - 2025-02-20

### Fixed

- Guard malformed og:video URL
- Hide speed when no bytes progress

## [4.0.1] - 2025-01-15

### Changed

- Bump version to 4.0.1

## [4.0.0] - 2024-12-10

### Changed

- Refactor: rename to MediaHub
- Enlarge design
- Center layout

## [3.0.0] - 2024-10-05

### Changed

- Refactor: rename to DropLoader
- Redesign UI
- Add icon

## [2.1.0] - 2024-08-20

### Added

- RU/EN localization with language switcher

## [2.0.0] - 2024-07-15

### Changed

- Target framework net9.0
- Update TikTokExplode to v1.1.1 single DLL
- Add Polly dependencies

## [1.0.0] - 2024-05-10

### Initial Release

- TikTok downloader
- YouTube downloader
- SoundCloud downloader
- VK downloader