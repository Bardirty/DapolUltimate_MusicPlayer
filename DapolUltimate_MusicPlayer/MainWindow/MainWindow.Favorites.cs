using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace DapolUltimate_MusicPlayer {
    public partial class MainWindow {
        private int userId;
        private List<TrackInfo> favoriteTracks = new List<TrackInfo>();
        private List<TrackStatInfo> topTracks = new List<TrackStatInfo>();

        public List<TrackInfo> FavoriteTracks {
            get => favoriteTracks;
            set { favoriteTracks = value; OnPropertyChanged(nameof(FavoriteTracks)); }
        }

        public List<TrackStatInfo> TopTracks {
            get => topTracks;
            set { topTracks = value; OnPropertyChanged(nameof(TopTracks)); }
        }

        private void LoadFavorites() {
            if (userId > 0)
                FavoriteTracks = dbService.GetUserFavorites(userId);
        }

        private void LoadStats() {
            TopTracks = dbService.GetTopPlayedTracks(10);
        }

        private void AddFavorite_Click(object sender, RoutedEventArgs e) {
            if (userId <= 0 || currentTrackIndex < 0 || currentTrackIndex >= playlistIds.Count)
                return;
            dbService.AddFavorite(userId, playlistIds[currentTrackIndex]);
            LoadFavorites();
        }

        private void RemoveFavorite_Click(object sender, RoutedEventArgs e) {
            if (userId <= 0 || FavoritesBox.SelectedItem is not TrackInfo track)
                return;
            dbService.RemoveFavorite(userId, track.Id);
            LoadFavorites();
        }

        private void FavoritesBox_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if (FavoritesBox.SelectedItem is TrackInfo track) {
                int index = playlistIds.IndexOf(track.Id);
                if (index == -1) {
                    playlistPaths.Add(track.Path);
                    playlistIds.Add(track.Id);
                    index = playlistIds.Count - 1;
                    OnPropertyChanged(nameof(PlaylistDisplayNames));
                }
                currentTrackIndex = index;
                LoadAndPlayFile(track.Path);
                PlaylistBox.SelectedIndex = currentTrackIndex;
            }
        }
    }
}
