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
    public partial class MainWindow : Window, INotifyPropertyChanged {
        private WaveOutEvent outputDevice;
        private AudioFileReader audioFile;
        private DispatcherTimer timer;
        private bool isPlaying = false;
        private bool isMuted = false;
        private double volumeBeforeMute = 0.5;

        // Храним полные пути к файлам
        private List<string> playlistPaths = new List<string>();

        // Отображаем только имена файлов без расширений
        public List<string> PlaylistDisplayNames =>
            playlistPaths.Select(Path.GetFileNameWithoutExtension).ToList();

        private int currentTrackIndex = -1;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow() {
            InitializeComponent();
            InitializeTimer();
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;
            DataContext = this;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e) {
            ApplyTheme("Aero");
            StatusText.Text = "Ready to play music";
        }

        private void ApplyTheme(string themeName) {
            Resources.MergedDictionaries.Clear();

            var baseDict = new ResourceDictionary();
            baseDict.Source = new Uri("/DapolUltimate_MusicPlayer;component/BaseStyles.xaml", UriKind.Relative);
            Resources.MergedDictionaries.Add(baseDict);

            var themeDict = new ResourceDictionary();
            themeDict.Source = new Uri($"/DapolUltimate_MusicPlayer;component/Themes/{themeName}Theme.xaml", UriKind.Relative);
            Resources.MergedDictionaries.Add(themeDict);

            Title = $"Dapol Ultimate Player - {themeName} Theme";
            StatusText.Text = $"{themeName} theme applied";
        }

        private void AeroTheme_Click(object sender, RoutedEventArgs e) {
            ApplyTheme("Aero");
        }

        private void FlatTheme_Click(object sender, RoutedEventArgs e) {
            ApplyTheme("Flat");
        }

        private void DarkTheme_Click(object sender, RoutedEventArgs e) {
            ApplyTheme("Dark");
        }

        private void InitializeTimer() {
            timer = new DispatcherTimer {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            timer.Tick += Timer_Tick;
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (audioFile == null) return;

            switch (e.Key) {
                case Key.Left:
                    PreviousButton_Click(null, null);
                    e.Handled = true;
                    break;
                case Key.Right:
                    NextButton_Click(null, null);
                    e.Handled = true;
                    break;
                case Key.Space:
                    PlayPauseButton_Click(null, null);
                    e.Handled = true;
                    break;
                case Key.M:
                    MuteButton_Click(null, null);
                    e.Handled = true;
                    break;
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
                outputDevice?.Stop();
                timer?.Stop();
                outputDevice?.Dispose();
                outputDevice = null;
                audioFile?.Dispose();
                audioFile = null;
            }
            catch (Exception ex) {
                Console.WriteLine($"Error stopping playback: {ex.Message}");
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
            if (audioFile == null) return;

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
                PlayPauseButton.Content = "⏯";
                StatusText.Text = $"Paused: {TrackTitle.Text}";
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
            PlayPauseButton.Content = "⏯";
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
            if (outputDevice == null) return;

            if (isMuted) {
                outputDevice.Volume = (float)volumeBeforeMute;
                VolumeSlider.Value = volumeBeforeMute;
                MuteButton.Content = "🔊";
                StatusText.Text = "Sound unmuted";
            }
            else {
                volumeBeforeMute = outputDevice.Volume;
                outputDevice.Volume = 0;
                VolumeSlider.Value = 0;
                MuteButton.Content = "🔇";
                StatusText.Text = "Sound muted";
            }

            isMuted = !isMuted;
        }

        private void SeekSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (audioFile == null) return;

            var slider = (Slider)sender;
            Point position = e.GetPosition(slider);
            double percent = position.X / slider.ActualWidth;
            double newValue = percent * slider.Maximum;

            slider.Value = newValue;
            audioFile.CurrentTime = TimeSpan.FromSeconds(newValue);
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (outputDevice != null) {
                outputDevice.Volume = (float)e.NewValue;

                if (e.NewValue <= 0) {
                    isMuted = true;
                    MuteButton.Content = "🔇";
                }
                else if (isMuted && e.NewValue > 0) {
                    isMuted = false;
                    MuteButton.Content = "🔊";
                }
            }
        }

        private void VolumeSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            var slider = (Slider)sender;
            Point position = e.GetPosition(slider);
            double percent = position.X / slider.ActualWidth;
            double newValue = percent * slider.Maximum;

            slider.Value = newValue;

            if (outputDevice != null) {
                outputDevice.Volume = (float)newValue;

                if (newValue <= 0) {
                    isMuted = true;
                    MuteButton.Content = "🔇";
                }
                else if (isMuted && newValue > 0) {
                    isMuted = false;
                    MuteButton.Content = "🔊";
                }
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
            // Проверяем, что есть что удалять и выбран корректный индекс
            if (playlistPaths.Count == 0 || PlaylistBox.SelectedIndex < 0 || PlaylistBox.SelectedIndex >= playlistPaths.Count) {
                return;
            }

            try {
                // Сохраняем выбранный индекс перед удалением
                int selectedIndex = PlaylistBox.SelectedIndex;

                // Проверяем, удаляем ли текущий играющий трек
                bool isRemovingCurrentTrack = (selectedIndex == currentTrackIndex);

                // Если удаляем текущий трек, останавливаем воспроизведение
                if (isRemovingCurrentTrack) {
                    StopPlayback();
                    ResetPlayerState();
                }

                // Удаляем трек из списка
                playlistPaths.RemoveAt(selectedIndex);
                OnPropertyChanged(nameof(PlaylistDisplayNames));

                // Обновляем текущий индекс
                if (currentTrackIndex > selectedIndex) {
                    currentTrackIndex--;
                }
                else if (currentTrackIndex == selectedIndex) {
                    currentTrackIndex = -1;
                }

                // Обновляем выделение в списке
                if (playlistPaths.Count > 0) {
                    // Выбираем следующий или предыдущий трек
                    int newSelectedIndex = selectedIndex >= playlistPaths.Count ? playlistPaths.Count - 1 : selectedIndex;
                    PlaylistBox.SelectedIndex = newSelectedIndex;

                    // Если удалили текущий трек и есть другие треки, воспроизводим новый
                    if (isRemovingCurrentTrack && newSelectedIndex >= 0) {
                        currentTrackIndex = newSelectedIndex;
                        LoadAndPlayFile(playlistPaths[currentTrackIndex]);
                    }
                }
                else {
                    // Если список пуст, сбрасываем выделение
                    PlaylistBox.SelectedIndex = -1;
                }

                StatusText.Text = "Track removed";
            }
            catch (Exception ex) {
                MessageBox.Show($"Error removing track: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) {
            this.Close();
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
                PlayPauseButton.Content = "⏯";
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

            // Возвращаем null, если обложка не найдена
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
                TotalTimeText.Text = FormatTime(audioFile.TotalTime);
                CurrentTimeText.Text = "00:00";

                // Извлечение обложки альбома
                var albumArt = GetAlbumArt(filePath);
                if (albumArt != null) {
                    AlbumArtImage.Source = albumArt;
                }
                else {
                    AlbumArtImage.Source = null; // Сбрасываем изображение, если обложка отсутствует
                }

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


    }
}