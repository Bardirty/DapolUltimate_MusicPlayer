using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DapolUltimate_MusicPlayer {
    public partial class MainWindow {
        private async Task SearchYouTubeAsync() {
            var query = YouTubeSearchBox.Text;
            if (string.IsNullOrWhiteSpace(query))
                return;

            StatusText.Text = "Searching YouTube...";
            try {
                YouTubeResults = await youTubeService.SearchVideosAsync(query);
                OnPropertyChanged(nameof(YouTubeResults));
                StatusText.Text = $"Found {YouTubeResults.Count} results";
            }
            catch (Exception ex) {
                MessageBox.Show($"Error searching YouTube: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Search failed";
            }
        }

        private async void YouTubeSearchButton_Click(object sender, RoutedEventArgs e) => await SearchYouTubeAsync();

        private async void YouTubeSearchBox_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                await SearchYouTubeAsync();
                e.Handled = true;
            }
        }

        private async void DownloadYouTubeButton_Click(object sender, RoutedEventArgs e) {
            if (sender is Button button && button.Tag is YouTubeVideo video) {
                var dialog = new SaveFileDialog {
                    Filter = "Audio files (*.mp3)|*.mp3",
                    FileName = $"{video.Title}.mp3"
                };
                if (dialog.ShowDialog() == true) {
                    StatusText.Text = "Downloading...";
                    try {
                        var path = await youTubeService.DownloadAudioAsync(video.Id, dialog.FileName);
                        playlistPaths.Add(path);
                        OnPropertyChanged(nameof(PlaylistDisplayNames));
                        StatusText.Text = "Download completed";
                    }
                    catch (Exception ex) {
                        MessageBox.Show($"Error downloading: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        StatusText.Text = "Download failed";
                    }
                }
            }
        }
    }
}
