using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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
                    var result = await youTubeService.DownloadAudioAsync(url, dialog.FileName);
                    if (!string.IsNullOrEmpty(result.Path)) {
                        playlistPaths.Add(result.Path);
                        currentTrackIndex = playlistPaths.Count - 1;
                        OnPropertyChanged(nameof(PlaylistDisplayNames));
                        PlaylistBox.SelectedIndex = currentTrackIndex;
                        LoadAndPlayFile(result.Path);
                        StatusText.Text = $"Downloaded: {video.Title}";
                    } else {
                        StatusText.Text = "Download failed";
                        if (!string.IsNullOrEmpty(result.Error))
                            MessageBox.Show(result.Error, "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void PlayYouTubeResult_Click(object sender, RoutedEventArgs e) {
            if (sender is Button button && button.Tag is Video video) {
                await PlayYouTubeVideoAsync(video);
            }
        }

        private async Task PlayYouTubeVideoAsync(Video video) {
            StatusText.Text = "Loading from YouTube...";
            var url = $"https://www.youtube.com/watch?v={video.Id}";
            var tempName = SanitizeFileName(video.Title) + ".tmp";
            var tempPath = Path.Combine(Path.GetTempPath(), tempName);
            var result = await youTubeService.DownloadAudioAsync(url, tempPath);
            if (!string.IsNullOrEmpty(result.Path)) {
                playlistPaths.Add(result.Path);
                currentTrackIndex = playlistPaths.Count - 1;
                OnPropertyChanged(nameof(PlaylistDisplayNames));
                PlaylistBox.SelectedIndex = currentTrackIndex;
                LoadAndPlayFile(result.Path);
                StatusText.Text = $"Now playing: {video.Title}";
            } else {
                StatusText.Text = "Playback failed";
                if (!string.IsNullOrEmpty(result.Error))
                    MessageBox.Show(result.Error, "Playback Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void YouTubeResultsBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            if (YouTubeResultsBox.SelectedItem is Video video) {
                await PlayYouTubeVideoAsync(video);
            }
        }
    }
}
