# DapolUltimate_MusicPlayer

This WPF music player provides simple playback features and customizable themes.

## Features
- Local audio file playback using NAudio
- SoundCloud track search and download
- **New:** integrated YouTube search with direct playback and audio download using [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode) 6.5.4.
  Downloads use safe filenames and tracks can be played immediately. Non-MP4 streams are converted to MP3 via *YoutubeExplode.Converter* and FFmpeg so they play correctly in NAudio.

Use the YouTube tab to search for videos. Each result now displays compact icon buttons (▶ and ↓) stacked vertically so they work across themes. You can also double‑click a result to play it. The list is scrollable and you can either download a track or play it instantly in the main player. Direct playback also adds the temporary file to the playlist so you can easily replay it.

Keyboard shortcuts (space to play/pause, arrows to skip) are disabled while typing in the search box so spaces can be entered normally.

The Flat theme has been expanded with custom styles for list boxes and other controls so switching themes no longer shows white backgrounds.

## Building

1. **Requirements**
   - .NET Framework 4.7.2
   - [Oracle Data Provider for .NET](https://www.oracle.com/database/technologies/) client libraries
   - FFmpeg in `PATH` for YouTube audio conversion

2. **Database credentials**
   - Set the environment variable `ORACLE_CONNECTION_STRING` to the Oracle connection string.
   - `App.config` contains a placeholder so credentials are not stored in source control.

3. **Restore packages and build**
   ```sh
   nuget restore
   msbuild DapolUltimate_MusicPlayer.sln
   ```

See the [LICENSE](LICENSE) file for license information.
