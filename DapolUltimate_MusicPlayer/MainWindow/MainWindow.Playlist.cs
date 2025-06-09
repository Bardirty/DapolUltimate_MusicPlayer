using System.Linq;
using System.Windows;
using Microsoft.Win32;
using System.IO;
using System;
using Microsoft.VisualBasic;

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

        private void PlaylistSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
            if (PlaylistSelector.SelectedIndex >= 0 && PlaylistSelector.SelectedIndex < playlists.Count) {
                currentPlaylistId = playlists[PlaylistSelector.SelectedIndex].Id;
                var tracks = dbService.LoadPlaylistTracks(currentPlaylistId);
                playlistPaths = tracks.Select(t => t.Path).ToList();
                playlistIds = tracks.Select(t => t.Id).ToList();
                OnPropertyChanged(nameof(PlaylistDisplayNames));
            }
        }

        private void AddPlaylist_Click(object sender, RoutedEventArgs e) {
            var name = Interaction.InputBox("Playlist name", "New Playlist", "");
            if (string.IsNullOrWhiteSpace(name)) return;
            var result = MessageBox.Show("Make playlist public?", "Visibility", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.Cancel) return;
            bool isPublic = result == MessageBoxResult.Yes;
            var id = dbService.AddPlaylist(userId, name, isPublic);
            playlists.Add(new PlaylistInfo { Id = id, Name = name, UserId = userId, IsPublic = isPublic });
            OnPropertyChanged(nameof(PlaylistNames));
            PlaylistSelector.SelectedIndex = playlists.Count - 1;
        }

        private void RenamePlaylist_Click(object sender, RoutedEventArgs e) {
            if (PlaylistSelector.SelectedIndex < 0 || PlaylistSelector.SelectedIndex >= playlists.Count)
                return;

            var current = playlists[PlaylistSelector.SelectedIndex];
            var name = Interaction.InputBox("Playlist name", "Rename Playlist", current.Name);
            if (string.IsNullOrWhiteSpace(name) || name == current.Name) return;

            dbService.RenamePlaylist(current.Id, name);
            current.Name = name;
            OnPropertyChanged(nameof(PlaylistNames));
            StatusText.Text = "Playlist renamed";
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
                    dbService.AddTrackToPlaylist(currentPlaylistId, id);
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

                dbService.DeleteTrack(currentPlaylistId, playlistIds[selectedIndex]);
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

        private void DeletePlaylist_Click(object sender, RoutedEventArgs e) {
            if (PlaylistSelector.SelectedIndex < 0 || PlaylistSelector.SelectedIndex >= playlists.Count) {
                return;
            }

            var result = MessageBox.Show("Delete this playlist?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            int index = PlaylistSelector.SelectedIndex;
            int id = playlists[index].Id;

            try {
                if (id == currentPlaylistId) {
                    StopPlayback();
                    ResetPlayerState();
                }

                dbService.DeletePlaylist(id);
                playlists.RemoveAt(index);
                OnPropertyChanged(nameof(PlaylistNames));

                if (playlists.Count > 0) {
                    int newIndex = index >= playlists.Count ? playlists.Count - 1 : index;
                    currentPlaylistId = playlists[newIndex].Id;
                    var tracks = dbService.LoadPlaylistTracks(currentPlaylistId);
                    playlistPaths = tracks.Select(t => t.Path).ToList();
                    playlistIds = tracks.Select(t => t.Id).ToList();
                    OnPropertyChanged(nameof(PlaylistDisplayNames));
                    PlaylistSelector.SelectedIndex = newIndex;
                }
                else {
                    currentPlaylistId = -1;
                    playlistPaths.Clear();
                    playlistIds.Clear();
                    OnPropertyChanged(nameof(PlaylistDisplayNames));
                    PlaylistSelector.SelectedIndex = -1;
                }

                StatusText.Text = "Playlist deleted";
            }
            catch (Exception ex) {
                MessageBox.Show($"Error deleting playlist: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
