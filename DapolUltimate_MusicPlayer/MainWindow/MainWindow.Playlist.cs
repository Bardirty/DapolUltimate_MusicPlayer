using System.Linq;
using System.Windows;
using Microsoft.Win32;
using System.IO;

namespace DapolUltimate_MusicPlayer {
    public partial class MainWindow {
        private void PlaylistBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
            if (PlaylistBox.SelectedIndex >= 0 &&
                PlaylistBox.SelectedIndex < playlistPaths.Count &&
                PlaylistBox.SelectedIndex != currentTrackIndex) {
                currentTrackIndex = PlaylistBox.SelectedIndex;
                LoadAndPlayFile(playlistPaths[currentTrackIndex]);
            }
        }

        private void AddToPlaylist_Click(object sender, RoutedEventArgs e) {
            var dialog = new OpenFileDialog {
                Filter = "Audio files (*.mp3;*.wav;*.aac;*.wma;*.flac)|*.mp3;*.wav;*.aac;*.wma;*.flac|All files (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true && dialog.FileNames.Length > 0) {
                var newPaths = dialog.FileNames.Except(playlistPaths).ToList();
                foreach (var path in newPaths) {
                    var id = dbService.AddTrack(new TrackInfo {
                        Title = Path.GetFileNameWithoutExtension(path),
                        Path = path,
                        IsYouTube = false,
                        CreatedAt = DateTime.Now
                    });
                    playlistPaths.Add(path);
                    playlistIds.Add(id);
                }
                OnPropertyChanged(nameof(PlaylistDisplayNames));
                StatusText.Text = $"Added {newPaths.Count} tracks";
            }
        }

        private void RemoveFromPlaylist_Click(object sender, RoutedEventArgs e) {
            if (playlistPaths.Count == 0 || PlaylistBox.SelectedIndex < 0 || PlaylistBox.SelectedIndex >= playlistPaths.Count) {
                return;
            }

            try {
                int selectedIndex = PlaylistBox.SelectedIndex;
                bool isRemovingCurrentTrack = (selectedIndex == currentTrackIndex);

                if (isRemovingCurrentTrack) {
                    StopPlayback();
                    ResetPlayerState();
                }

                dbService.DeleteTrack(playlistIds[selectedIndex]);
                playlistPaths.RemoveAt(selectedIndex);
                playlistIds.RemoveAt(selectedIndex);
                OnPropertyChanged(nameof(PlaylistDisplayNames));

                if (currentTrackIndex > selectedIndex) {
                    currentTrackIndex--;
                }
                else if (currentTrackIndex == selectedIndex) {
                    currentTrackIndex = -1;
                }

                if (playlistPaths.Count > 0) {
                    int newSelectedIndex = selectedIndex >= playlistPaths.Count ? playlistPaths.Count - 1 : selectedIndex;
                    PlaylistBox.SelectedIndex = newSelectedIndex;

                    if (isRemovingCurrentTrack && newSelectedIndex >= 0) {
                        currentTrackIndex = newSelectedIndex;
                        LoadAndPlayFile(playlistPaths[currentTrackIndex]);
                    }
                }
                else {
                    PlaylistBox.SelectedIndex = -1;
                }

                StatusText.Text = "Track removed";
            }
            catch (System.Exception ex) {
                MessageBox.Show($"Error removing track: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
