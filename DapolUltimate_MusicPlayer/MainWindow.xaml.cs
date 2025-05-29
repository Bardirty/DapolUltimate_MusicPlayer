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

        private void ApplyTheme(string themeName) {
            try {
                Resources.MergedDictionaries.Clear();

                var baseDict = new ResourceDictionary {
                    Source = new Uri("pack://application:,,,/DapolUltimate_MusicPlayer;component/BaseStyles.xaml")
                };
                Resources.MergedDictionaries.Add(baseDict);

                var themeDict = new ResourceDictionary();
                switch (themeName.ToLower()) {
                    case "aero":
                        themeDict.Source = new Uri("pack://application:,,,/DapolUltimate_MusicPlayer;component/Themes/AeroTheme.xaml");
                        break;
                    case "flat":
                        themeDict.Source = new Uri("pack://application:,,,/DapolUltimate_MusicPlayer;component/Themes/FlatTheme.xaml");
                        break;
                    case "dark":
                        themeDict.Source = new Uri("pack://application:,,,/DapolUltimate_MusicPlayer;component/Themes/DarkTheme.xaml");
                        break;
                    default:
                        themeDict.Source = new Uri("pack://application:,,,/DapolUltimate_MusicPlayer;component/Themes/AeroTheme.xaml");
                        break;
                }
                Resources.MergedDictionaries.Add(themeDict);

                Properties.Settings.Default.SelectedTheme = themeName;
                Properties.Settings.Default.Save();

                StatusText.Text = $"{themeName} theme applied";
            }
            catch (Exception ex) {
                MessageBox.Show($"Error applying theme: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeTimer() {
            timer = new DispatcherTimer {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            timer.Tick += Timer_Tick;
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Space) {
                PlayPauseButton_Click(null, null);
                e.Handled = true;
            }
            else if (e.Key == Key.Left) {
                PreviousButton_Click(null, null);
                e.Handled = true;
            }
            else if (e.Key == Key.Right) {
                NextButton_Click(null, null);
                e.Handled = true;
            }
            else if (e.Key == Key.M) {
                MuteButton_Click(null, null);
                e.Handled = true;
            }
        }

        private void LoadFileButton_Click(object sender, RoutedEventArgs e) {
            var dialog = new OpenFileDialog {
                Filter = "Audio files (*.mp3;*.wav;*.aac;*.wma)|*.mp3;*.wav;*.aac;*.wma|All files (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true) {
                playlistPaths = dialog.FileNames.ToList();
                currentTrackIndex = 0;
                LoadAndPlayFile(playlistPaths[currentTrackIndex]);
                PlaylistBox.SelectedIndex = currentTrackIndex;
                OnPropertyChanged(nameof(PlaylistDisplayNames));
                StatusText.Text = $"Loaded {playlistPaths.Count} tracks";
            }
        }

        private void StopPlayback() {
            try {
                timer?.Stop();

                if (outputDevice != null) {
                    outputDevice.Stop();
                    outputDevice.Dispose();
                    outputDevice = null;
                }

                if (audioFile != null) {
                    audioFile.Dispose();
                    audioFile = null;
                }

                isPlaying = false;
            }
            catch (Exception ex) {
                Debug.WriteLine($"Error stopping playback: {ex}");
            }
        }

        private void PlayPreviousTrack() {
            if (playlistPaths.Count == 0) return;

            currentTrackIndex--;
            if (currentTrackIndex < 0) {
                currentTrackIndex = playlistPaths.Count - 1;
            }

            LoadAndPlayFile(playlistPaths[currentTrackIndex]);
            PlaylistBox.SelectedIndex = currentTrackIndex;
        }

        private void PlayNextTrack() {
            if (playlistPaths.Count == 0) return;

            currentTrackIndex++;
            if (currentTrackIndex >= playlistPaths.Count) {
                currentTrackIndex = 0;
            }

            LoadAndPlayFile(playlistPaths[currentTrackIndex]);
            PlaylistBox.SelectedIndex = currentTrackIndex;
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e) {
            if (audioFile != null) {
                if (!isPlaying) {
                    outputDevice.Play();
                    timer.Start();
                    isPlaying = true;
                    PlayPauseButton.Content = "⏸";
                    StatusText.Text = $"Resumed: {TrackTitle.Text}";
                }
                else {
                    outputDevice.Pause();
                    timer.Stop();
                    isPlaying = false;
                    PlayPauseButton.Content = "▶";
                    StatusText.Text = $"Paused: {TrackTitle.Text}";
                }
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e) {
            StopPlayback();

            SeekSlider.Value = 0;
            isPlaying = false;
            TrackTitle.Text = "No track loaded";
            TrackArtist.Text = "Unknown Artist";
            CurrentTimeText.Text = "00:00";
            TotalTimeText.Text = "00:00";
            PlayPauseButton.Content = "▶";
            currentTrackIndex = -1;
            PlaylistBox.SelectedIndex = -1;
            StatusText.Text = "Playback stopped";
        }

        private void Timer_Tick(object sender, EventArgs e) {
            if (audioFile != null) {
                SeekSlider.Value = audioFile.CurrentTime.TotalSeconds;
                CurrentTimeText.Text = FormatTime(audioFile.CurrentTime);

                if (audioFile.CurrentTime.TotalSeconds >= audioFile.TotalTime.TotalSeconds - 0.5) {
                    PlayNextTrack();
                }
            }
        }

        private void SeekSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (audioFile != null && Math.Abs(audioFile.CurrentTime.TotalSeconds - e.NewValue) > 1) {
                audioFile.CurrentTime = TimeSpan.FromSeconds(e.NewValue);
            }
        }

        private string FormatTime(TimeSpan time) {
            return string.Format("{0:D2}:{1:D2}", (int)time.TotalMinutes, time.Seconds);
        }

        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            StopPlayback();
            timer?.Stop();
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e) {
            if (playlistPaths.Count > 0) {
                PlayPreviousTrack();
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e) {
            if (playlistPaths.Count > 0) {
                PlayNextTrack();
            }
        }

        private void MuteButton_Click(object sender, RoutedEventArgs e) {
            if (outputDevice != null) {
                if (isMuted) {
                    outputDevice.Volume = (float)volumeBeforeMute;
                    VolumeSlider.Value = volumeBeforeMute;
                }
                else {
                    volumeBeforeMute = outputDevice.Volume;
                    outputDevice.Volume = 0;
                    VolumeSlider.Value = 0;
                }
            }

            isMuted = !isMuted;
            MuteButton.Content = isMuted ? "🔇" : "🔊";
            StatusText.Text = isMuted ? "Sound muted" : "Sound unmuted";
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (outputDevice != null) {
                outputDevice.Volume = (float)e.NewValue;
            }

            if (e.NewValue <= 0) {
                isMuted = true;
                MuteButton.Content = "🔇";
            }
            else if (isMuted && e.NewValue > 0) {
                isMuted = false;
                MuteButton.Content = "🔊";
            }
        }

        private void PlaylistBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
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
                playlistPaths.AddRange(dialog.FileNames.Except(playlistPaths));
                OnPropertyChanged(nameof(PlaylistDisplayNames));
                StatusText.Text = $"Added {dialog.FileNames.Length} tracks";
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

                playlistPaths.RemoveAt(selectedIndex);
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
            catch (Exception ex) {
                MessageBox.Show($"Error removing track: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetPlayerState() {
            try {
                StopPlayback();

                SeekSlider.Value = 0;
                isPlaying = false;
                TrackTitle.Text = "No track loaded";
                TrackArtist.Text = "Unknown Artist";
                CurrentTimeText.Text = "00:00";
                TotalTimeText.Text = "00:00";
                PlayPauseButton.Content = "▶";
                currentTrackIndex = -1;
                PlaylistBox.SelectedIndex = -1;
            }
            catch (Exception ex) {
                Console.WriteLine($"Error resetting player state: {ex.Message}");
            }
        }

        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ImageSource GetAlbumArt(string filePath) {
            try {
                var file = TagLib.File.Create(filePath);
                var pictures = file.Tag.Pictures;

                if (pictures != null && pictures.Length > 0) {
                    using (var stream = new MemoryStream(pictures[0].Data.Data)) {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                        return bitmap;
                    }
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"Error extracting album art: {ex.Message}");
            }

            return null;
        }

        private void LoadAndPlayFile(string filePath) {
            try {
                StopPlayback();

                if (!File.Exists(filePath)) {
                    StatusText.Text = "File not found";
                    PlayNextTrack();
                    return;
                }

                audioFile = new AudioFileReader(filePath);
                outputDevice = new WaveOutEvent();
                outputDevice.Init(audioFile);
                outputDevice.Volume = (float)VolumeSlider.Value;

                TrackTitle.Text = Path.GetFileNameWithoutExtension(filePath);
                TrackArtist.Text = Path.GetDirectoryName(filePath);
                SeekSlider.Maximum = audioFile.TotalTime.TotalSeconds;
                SeekSlider.Value = 0;
                SeekSlider.IsEnabled = true;
                TotalTimeText.Text = FormatTime(audioFile.TotalTime);
                CurrentTimeText.Text = "00:00";

                var albumArt = GetAlbumArt(filePath);
                AlbumArtImage.Source = albumArt;

                outputDevice.Play();
                timer.Start();
                isPlaying = true;
                PlayPauseButton.Content = "⏸";

                StatusText.Text = $"Now playing: {TrackTitle.Text}";
            }
            catch (Exception ex) {
                MessageBox.Show($"Error loading file: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
                PlayNextTrack();
            }
        }

        private void AeroTheme_Click(object sender, RoutedEventArgs e) => ApplyTheme("Aero");
        private void FlatTheme_Click(object sender, RoutedEventArgs e) => ApplyTheme("Flat");
        private void DarkTheme_Click(object sender, RoutedEventArgs e) => ApplyTheme("Dark");
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}