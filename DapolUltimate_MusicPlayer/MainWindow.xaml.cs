using Microsoft.Win32;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace DapolUltimate_MusicPlayer {
    public partial class MainWindow : Window {
        private WaveOutEvent outputDevice;
        private AudioFileReader audioFile;
        private DispatcherTimer timer;
        private bool isPlaying = false;
        private List<string> playlist = new List<string>();
        private int currentTrackIndex = -1;
        private bool isMuted = false;
        private double volumeBeforeMute = 0.5;

        public MainWindow() {
            InitializeComponent();
            InitializeTimer();
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;
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
            }
        }

        private void LoadFileButton_Click(object sender, RoutedEventArgs e) {
            var dialog = new OpenFileDialog {
                Filter = "Audio files (*.mp3;*.wav)|*.mp3;*.wav|All files (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true) {
                playlist = dialog.FileNames.ToList();
                currentTrackIndex = 0;
                LoadAndPlayFile(playlist[currentTrackIndex]);
            }
        }

        private void LoadAndPlayFile(string filePath) {
            try {
                outputDevice?.Stop();
                outputDevice?.Dispose();
                audioFile?.Dispose();

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

                outputDevice.Play();
                timer.Start();
                isPlaying = true;
                PlayPauseButton.Content = "⏸ PAUSE";
            }
            catch (Exception ex) {
                MessageBox.Show($"Error loading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                PlayNextTrack();
            }
        }

        private void PlayPreviousTrack() {
            if (playlist.Count == 0) return;

            currentTrackIndex--;
            if (currentTrackIndex < 0) {
                currentTrackIndex = playlist.Count - 1;
            }

            LoadAndPlayFile(playlist[currentTrackIndex]);
        }

        private void PlayNextTrack() {
            if (playlist.Count == 0) return;

            currentTrackIndex++;
            if (currentTrackIndex >= playlist.Count) {
                currentTrackIndex = 0;
            }

            LoadAndPlayFile(playlist[currentTrackIndex]);
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e) {
            if (audioFile == null) return;

            if (!isPlaying) {
                outputDevice.Play();
                timer.Start();
                isPlaying = true;
                PlayPauseButton.Content = "⏸ PAUSE";
            }
            else {
                outputDevice.Pause();
                timer.Stop();
                isPlaying = false;
                PlayPauseButton.Content = "⏯ PLAY";
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e) {
            if (outputDevice != null) {
                outputDevice.Stop();
                timer.Stop();
                outputDevice.Dispose();
                outputDevice = null;
            }

            if (audioFile != null) {
                audioFile.Dispose();
                audioFile = null;
            }

            SeekSlider.Value = 0;
            isPlaying = false;
            TrackTitle.Text = "No track loaded";
            TrackArtist.Text = "Unknown Artist";
            CurrentTimeText.Text = "00:00";
            TotalTimeText.Text = "00:00";
            PlayPauseButton.Content = "⏯ PLAY";
            currentTrackIndex = -1;
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
            outputDevice?.Dispose();
            audioFile?.Dispose();
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e) {
            if (playlist.Count > 0) {
                PlayPreviousTrack();
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e) {
            if (playlist.Count > 0) {
                PlayNextTrack();
            }
        }

        private void MuteButton_Click(object sender, RoutedEventArgs e) {
            if (outputDevice == null) return;

            if (isMuted) {
                outputDevice.Volume = (float)volumeBeforeMute;
                VolumeSlider.Value = volumeBeforeMute;
                MuteButton.Content = "🔊";
            }
            else {
                volumeBeforeMute = outputDevice.Volume;
                outputDevice.Volume = 0;
                VolumeSlider.Value = 0;
                MuteButton.Content = "🔇";
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

        private void VolumeSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            var slider = (Slider)sender;
            Point position = e.GetPosition(slider);
            double percent = position.X / slider.ActualWidth;
            double newValue = percent * slider.Maximum;

            slider.Value = newValue;
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
    }
}
