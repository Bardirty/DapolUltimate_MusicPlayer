using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using YoutubeExplode.Videos;

namespace DapolUltimate_MusicPlayer {
    public partial class MainWindow {
        private readonly YouTubeService youTubeService = new YouTubeService();
        private List<Video> youtubeResults = new List<Video>();

        public List<Video> YouTubeResults {
            get => youtubeResults;
            set {
                youtubeResults = value;
                OnPropertyChanged(nameof(YouTubeResults));
            }
        }

        private async void YouTubeSearchButton_Click(object sender, RoutedEventArgs e) {
            var query = YouTubeSearchBox.Text.Trim();
            if (string.IsNullOrEmpty(query)) return;

            StatusText.Text = "Searching YouTube...";
            try {
                var results = await youTubeService.SearchVideosAsync(query);
                YouTubeResults = results;
                StatusText.Text = $"Found {results.Count} videos";
            }
            catch (Exception ex) {
                MessageBox.Show($"Error searching YouTube: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Search failed";
            }
        }

        private string SanitizeFileName(string name) {
            foreach (var c in Path.GetInvalidFileNameChars()) {
                name = name.Replace(c, '_');
            }
            return name;
        }

        private async void DownloadYouTubeResult_Click(object sender, RoutedEventArgs e) {
            if (sender is Button button && button.Tag is Video video) {
                var dialog = new SaveFileDialog {
                    Filter = "Audio files (*.mp3;*.m4a;*.webm)|*.mp3;*.m4a;*.webm|All files (*.*)|*.*",
                    FileName = SanitizeFileName(video.Title),
                    DefaultExt = ".mp3"
                };

                if (dialog.ShowDialog() == true) {
                    StatusText.Text = "Downloading...";
                    var url = $"https://www.youtube.com/watch?v={video.Id}";
                    var path = await youTubeService.DownloadAudioAsync(url, dialog.FileName);
                    if (!string.IsNullOrEmpty(path)) {
                        playlistPaths.Add(path);
                        currentTrackIndex = playlistPaths.Count - 1;
                        OnPropertyChanged(nameof(PlaylistDisplayNames));
                        PlaylistBox.SelectedIndex = currentTrackIndex;
                        LoadAndPlayFile(path);
                        StatusText.Text = $"Downloaded: {video.Title}";
                    } else {
                        StatusText.Text = "Download failed";
                    }
                }
            }
        }

        private async void PlayYouTubeResult_Click(object sender, RoutedEventArgs e) {
            if (sender is Button button && button.Tag is Video video) {
                StatusText.Text = "Loading from YouTube...";
                var url = $"https://www.youtube.com/watch?v={video.Id}";
                var tempName = SanitizeFileName(video.Title) + ".tmp";
                var tempPath = Path.Combine(Path.GetTempPath(), tempName);
                var path = await youTubeService.DownloadAudioAsync(url, tempPath);
                if (!string.IsNullOrEmpty(path)) {
                    playlistPaths.Add(path);
                    currentTrackIndex = playlistPaths.Count - 1;
                    OnPropertyChanged(nameof(PlaylistDisplayNames));
                    PlaylistBox.SelectedIndex = currentTrackIndex;
                    LoadAndPlayFile(path);
                    StatusText.Text = $"Now playing: {video.Title}";
                } else {
                    StatusText.Text = "Playback failed";
                }
            }
        }
    }
}
