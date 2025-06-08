# DapolUltimate_MusicPlayer

This WPF music player provides simple playback features and customizable themes.

## Features
- Local audio file playback using NAudio
- SoundCloud track search and download
- **New:** integrated YouTube search with direct playback and audio download using [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode) 6.5.4.
  Downloads use safe filenames and tracks can be played immediately.
- **New:** create your own playlists. All tracks and playlist membership are stored in an Oracle database so your lists persist between sessions.
- **New:** playlists can be renamed and the changes are saved back to the database.

Use the YouTube tab to search for videos. Each result now displays compact icon buttons (▶ and ↓) stacked vertically so they work across themes. You can also double‑click a result to play it. The list is scrollable and you can either download a track or play it instantly in the main player. Direct playback also adds the temporary file to the playlist so you can easily replay it.

Keyboard shortcuts (space to play/pause, arrows to skip) are disabled while typing in the search box so spaces can be entered normally.

The Flat theme now includes styles for buttons and combo boxes so no controls appear unstyled when switching themes.
