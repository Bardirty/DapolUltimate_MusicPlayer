using Microsoft.Win32;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.VisualBasic;

namespace DapolUltimate_MusicPlayer {
    public enum AppTheme {
        Aero,
        Flat,
        Dark
    }

    public partial class MainWindow : Window, INotifyPropertyChanged {
        private WaveOutEvent outputDevice;
        private AudioFileReader audioFile;
        private DispatcherTimer timer;
        private bool isPlaying = false;
        private bool isMuted = false;
        private double volumeBeforeMute = 0.5;
        private List<string> playlistPaths = new List<string>();
        private List<int> playlistIds = new List<int>();
        private int currentTrackIndex = -1;
        private readonly OracleDbService dbService = new OracleDbService();

        private List<PlaylistInfo> playlists = new List<PlaylistInfo>();
        private int currentPlaylistId = -1;

        public List<string> PlaylistDisplayNames =>
            playlistPaths.Select(Path.GetFileNameWithoutExtension).ToList();

        public event PropertyChangedEventHandler PropertyChanged;

        public List<string> PlaylistNames => playlists.ConvertAll(p => p.Name);

        public MainWindow() {
            InitializeComponent();

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                LogError((Exception)e.ExceptionObject);
            Dispatcher.UnhandledException += (s, e) => {
                LogError(e.Exception);
                e.Handled = true;
            };

            if (!string.IsNullOrEmpty(Properties.Settings.Default.SelectedTheme)) {
                ApplyTheme(Properties.Settings.Default.SelectedTheme);
            }
            else {
                ApplyTheme("Aero");
            }

            InitializeTimer();
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;
            DataContext = this;

            try {
                dbService.EnsureTableExists();

                playlists = dbService.LoadPlaylists();
                if (playlists.Count == 0) {
                    var id = dbService.AddPlaylist("Default");
                    playlists.Add(new PlaylistInfo { Id = id, Name = "Default" });
                }
                OnPropertyChanged(nameof(PlaylistNames));

                currentPlaylistId = playlists[0].Id;
                var tracks = dbService.LoadPlaylistTracks(currentPlaylistId);
                playlistPaths = tracks.Select(t => t.Path).ToList();
                playlistIds = tracks.Select(t => t.Id).ToList();
                OnPropertyChanged(nameof(PlaylistDisplayNames));
                PlaylistSelector.SelectedIndex = 0;
            }
            catch (Exception ex) {
                LogError(ex);
            }
        }

        private void LogError(Exception ex) {
            File.AppendAllText("error.log", $"{DateTime.Now}: {ex}\n\n");
            MessageBox.Show($"Critical error: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}