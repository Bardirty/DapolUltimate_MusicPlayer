using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        private bool isSoundCloudPlaying = false;

        // Добавляем класс для представления результатов поиска SoundCloud
        public class SoundCloudTrack {
            public string Title { get; set; }
            public string Url { get; set; }
            public string ArtworkUrl { get; set; }
            public SoundCloudUser User { get; set; }

            public SoundCloudTrack() {
                User = new SoundCloudUser();
            }
        }

        public class SoundCloudUser {
            public string Username { get; set; }
        }

        public List<SoundCloudTrack> SearchResults { get; set; } = new List<SoundCloudTrack>();

        public List<string> PlaylistDisplayNames =>
            playlistPaths.Select(Path.GetFileNameWithoutExtension).ToList();

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow() {
            InitializeComponent();

            // Добавьте это в конструктор
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
                ApplyTheme("Aero"); // Тема по умолчанию
            }
            InitializeWebView2();
            InitializeTimer();
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;
            DataContext = this;
        }

        private void LogError(Exception ex) {
            File.AppendAllText("error.log", $"{DateTime.Now}: {ex}\n\n");
            MessageBox.Show($"Critical error: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private async void InitializeWebView2() {
            try {
                var env = await CoreWebView2Environment.CreateAsync();
                await SoundCloudWebView.EnsureCoreWebView2Async(env);
                SoundCloudWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                SoundCloudWebView.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;
            }
            catch (Exception ex) {
                MessageBox.Show($"WebView2 initialization failed: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e) {
            if (SoundCloudWebView?.CoreWebView2 == null) return;
            try {
                var message = e.TryGetWebMessageAsString();
                Dispatcher.Invoke(() => {
                    switch (message) {
                        case "PLAYING":
                            isSoundCloudPlaying = true;
                            PlayPauseButton.Content = "⏸︎";
                            StatusText.Text = "SoundCloud track playing";
                            break;
                        case "PAUSED":
                            isSoundCloudPlaying = false;
                            PlayPauseButton.Content = "▶";
                            StatusText.Text = "SoundCloud track paused";
                            break;
                        case "FINISHED":
                            PlayNextTrack();
                            break;
                    }
                });
            }
            catch (Exception ex) {
                Debug.WriteLine($"WebView message error: {ex}");
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e) {
            ApplyTheme("Aero");
            StatusText.Text = "Ready to play music";
            LoadSoundCloudWidget();
        }

        private void LoadSoundCloudWidget() {
            string html = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body { margin: 0; padding: 0; background-color: transparent; }
                        #widget-container { width: 100%; height: 100%; }
                    </style>
                </head>
                <body>
                    <div id='widget-container'></div>
                    <script src='https://w.soundcloud.com/player/api.js'></script>
                    <script>
                        let widget;
                        
                        function loadWidget(trackUrl) {
                            const container = document.getElementById('widget-container');
                            container.innerHTML = '';
                            
                            const iframe = document.createElement('iframe');
                            iframe.id = 'sc-widget';
                            iframe.width = '100%';
                            iframe.height = '100%';
                            iframe.style.border = 'none';
                            iframe.src = 'https://w.soundcloud.com/player/?url=' + encodeURIComponent(trackUrl || '') + 
                                          '&color=%23ff5500&auto_play=false&hide_related=true&show_comments=false&show_user=false&show_reposts=false&show_teaser=false';
                            
                            container.appendChild(iframe);
                            
                            widget = SC.Widget('sc-widget');
                            
                            widget.bind(SC.Widget.Events.READY, function() {
                                window.chrome.webview.postMessage('READY');
                            });
                            
                            widget.bind(SC.Widget.Events.PLAY, function() {
                                window.chrome.webview.postMessage('PLAYING');
                            });
                            
                            widget.bind(SC.Widget.Events.PAUSE, function() {
                                window.chrome.webview.postMessage('PAUSED');
                            });
                            
                            widget.bind(SC.Widget.Events.FINISH, function() {
                                window.chrome.webview.postMessage('FINISHED');
                            });
                        }
                        
                        function playTrack(trackUrl) {
                            if (!widget) loadWidget(trackUrl);
                            else {
                                widget.load(trackUrl, {
                                    auto_play: true,
                                    show_artwork: true
                                });
                            }
                        }
                        
                        function togglePlay() {
                            if (widget) {
                                widget.toggle();
                            }
                        }
                        
                        function seekTo(seconds) {
                            if (widget) {
                                widget.seekTo(seconds * 1000);
                            }
                        }
                        
                        function setVolume(volume) {
                            if (widget) {
                                widget.setVolume(Math.floor(volume * 100));
                            }
                        }
                        
                        // Initial load
                        loadWidget();
                    </script>
                </body>
                </html>";

            SoundCloudWebView.NavigateToString(html);
        }

        private void PlaySoundCloudTrack(string trackUrl) {
            try {
                StopPlayback(); // Stop any local playback

                string script = $"playTrack('{trackUrl}');";
                SoundCloudWebView.CoreWebView2.ExecuteScriptAsync(script);

                TrackTitle.Text = "SoundCloud Track";
                TrackArtist.Text = "SoundCloud Artist";
                StatusText.Text = "Loading SoundCloud track...";

                // Disable seek slider for SoundCloud tracks
                SeekSlider.IsEnabled = false;
                SeekSlider.Value = 0;
            }
            catch (Exception ex) {
                MessageBox.Show($"Error playing SoundCloud track: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Замените метод ApplyTheme на следующую реализацию:
        private void ApplyTheme(string themeName) {
            try {
                Resources.MergedDictionaries.Clear();

                // Добавляем базовые стили
                var baseDict = new ResourceDictionary {
                    Source = new Uri("pack://application:,,,/DapolUltimate_MusicPlayer;component/BaseStyles.xaml")
                };
                Resources.MergedDictionaries.Add(baseDict);

                // Добавляем тему
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

                // Сохраняем тему
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
            if (isSoundCloudPlaying) {
                // Control SoundCloud playback
                SoundCloudWebView.CoreWebView2.ExecuteScriptAsync("togglePlay();");
            }
            else if (audioFile != null) {
                // Control local file playback
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
            SoundCloudWebView.CoreWebView2.ExecuteScriptAsync("playTrack('');"); // Clear SoundCloud player

            SeekSlider.Value = 0;
            isPlaying = false;
            isSoundCloudPlaying = false;
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
            if (isSoundCloudPlaying) {
                // Mute SoundCloud player
                string script = $"setVolume({(isMuted ? volumeBeforeMute : 0)});";
                SoundCloudWebView.CoreWebView2.ExecuteScriptAsync(script);
            }
            else if (outputDevice != null) {
                // Mute local player
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
            if (isSoundCloudPlaying) {
                // Set SoundCloud volume
                string script = $"setVolume({e.NewValue});";
                SoundCloudWebView.CoreWebView2.ExecuteScriptAsync(script);
            }
            else if (outputDevice != null) {
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
                isSoundCloudPlaying = false;

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

        private void SearchSoundCloud_Click(object sender, RoutedEventArgs e) {
            if (string.IsNullOrWhiteSpace(SearchBox.Text)) {
                StatusText.Text = "Please enter search query";
                return;
            }

            string searchUrl = $"https://soundcloud.com/search?q={Uri.EscapeDataString(SearchBox.Text)}";
            SoundCloudWebView.CoreWebView2.Navigate(searchUrl);
            StatusText.Text = $"Searching SoundCloud for: {SearchBox.Text}";

            // После перехода по URL ищем результаты и наполняем SearchResultsBox
            // Обычно это делается через API SoundCloud, но для простоты здесь просто симулируем результаты
            GenerateDummySearchResults(SearchBox.Text);
        }

        // Метод для создания примерных результатов поиска (в реальном приложении здесь был бы API-запрос)
        private void GenerateDummySearchResults(string query) {
            SearchResults.Clear();

            // Создаем несколько демонстрационных результатов
            for (int i = 1; i <= 5; i++) {
                SearchResults.Add(new SoundCloudTrack {
                    Title = $"{query} - Track {i}",
                    Url = $"https://soundcloud.com/demo/track{i}",
                    ArtworkUrl = "https://via.placeholder.com/50",
                    User = new SoundCloudUser { Username = $"Artist {i}" }
                });
            }

            // Привязываем результаты к SearchResultsBox
            SearchResultsBox.ItemsSource = SearchResults;
            StatusText.Text = $"Found {SearchResults.Count} tracks";
        }

        // Добавление недостающего обработчика событий
        private void SearchResultsBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (SearchResultsBox.SelectedItem != null) {
                var track = SearchResultsBox.SelectedItem as SoundCloudTrack;
                if (track != null) {
                    SoundCloudUrlBox.Text = track.Url;
                    StatusText.Text = $"Selected: {track.Title}";
                }
            }
        }

        private void PlaySoundCloudUrl_Click(object sender, RoutedEventArgs e) {
            if (!string.IsNullOrWhiteSpace(SoundCloudUrlBox.Text)) {
                if (SoundCloudUrlBox.Text.Contains("soundcloud.com")) {
                    PlaySoundCloudTrack(SoundCloudUrlBox.Text);
                    StatusText.Text = "Loading SoundCloud track...";
                }
                else {
                    StatusText.Text = "Please enter a valid SoundCloud URL";
                }
            }
        }

        // Добавление недостающего обработчика для добавления в плейлист
        private void AddSoundCloudToPlaylist_Click(object sender, RoutedEventArgs e) {
            if (SearchResultsBox.SelectedItem != null) {
                var track = SearchResultsBox.SelectedItem as SoundCloudTrack;
                if (track != null) {
                    // В реальном приложении здесь было бы добавление трека из SoundCloud в локальный плейлист
                    // Но так как мы не можем скачать трек напрямую (требуется API), добавим только уведомление
                    MessageBox.Show($"Selected track '{track.Title}' can be played directly from SoundCloud.\n\n" +
                                    $"For permanent addition to your library, use the Download button first.",
                                    "Add to Playlist",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);

                    StatusText.Text = $"Selected track would be added to playlist after download";
                }
            }
            else {
                StatusText.Text = "Select a track first";
            }
        }

        // Добавление недостающего обработчика для скачивания
        private void DownloadSoundCloudTrack_Click(object sender, RoutedEventArgs e) {
            if (SearchResultsBox.SelectedItem != null) {
                var track = SearchResultsBox.SelectedItem as SoundCloudTrack;
                if (track != null) {
                    // В реальном приложении здесь был бы код для скачивания трека
                    // Для этого требуется SoundCloud API-ключ и соответствующие разрешения

                    StatusText.Text = $"Simulating download of: {track.Title}";

                    // Показываем диалог сохранения
                    var dialog = new SaveFileDialog {
                        FileName = SanitizeFileName(track.Title) + ".mp3",
                        Filter = "MP3 files (*.mp3)|*.mp3",
                        Title = "Save SoundCloud track"
                    };

                    if (dialog.ShowDialog() == true) {
                        // Эмулируем процесс скачивания
                        MessageBox.Show($"Track would be downloaded to: {dialog.FileName}\n\n" +
                                        "Note: Actual downloading requires SoundCloud API credentials.",
                                        "Download Simulation",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Information);

                        StatusText.Text = $"Download simulation complete";
                    }
                }
            }
            else {
                StatusText.Text = "Select a track first";
            }
        }

        // Вспомогательный метод для создания безопасного имени файла
        private string SanitizeFileName(string fileName) {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return Regex.Replace(fileName, invalidRegStr, "_");
        }

        private void AeroTheme_Click(object sender, RoutedEventArgs e) => ApplyTheme("Aero");
        private void FlatTheme_Click(object sender, RoutedEventArgs e) => ApplyTheme("Flat");
        private void DarkTheme_Click(object sender, RoutedEventArgs e) => ApplyTheme("Dark");
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}