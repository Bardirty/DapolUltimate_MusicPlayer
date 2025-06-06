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
        private int currentTrackIndex = -1;
        private readonly YouTubeService youTubeService = new YouTubeService();
        public System.Collections.Generic.List<YouTubeVideo> YouTubeResults { get; private set; } = new System.Collections.Generic.List<YouTubeVideo>();

        public List<string> PlaylistDisplayNames =>
            playlistPaths.Select(Path.GetFileNameWithoutExtension).ToList();

        public event PropertyChangedEventHandler PropertyChanged;

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