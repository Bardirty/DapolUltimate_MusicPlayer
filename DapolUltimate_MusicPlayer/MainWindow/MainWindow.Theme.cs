using System;
using System.Windows;

namespace DapolUltimate_MusicPlayer {
    public partial class MainWindow {
        private void ApplyTheme(string themeName) {
            try {
                Resources.MergedDictionaries.Clear();

                var baseDict = new ResourceDictionary {
                    Source = new Uri("pack://application:,,,/DapolUltimate_MusicPlayer;component/BaseStyles.xaml")
                };
                Resources.MergedDictionaries.Add(baseDict);

                var themeDict = new ResourceDictionary();
                var themePath = $"Themes/{themeName}Theme.xaml";
                var uri = new Uri($"pack://application:,,,/DapolUltimate_MusicPlayer;component/{themePath}");

                try {
                    themeDict.Source = uri;
                }
                catch {
                    // fallback to Aero if the requested theme doesn't exist
                    themeDict.Source = new Uri("pack://application:,,,/DapolUltimate_MusicPlayer;component/Themes/AeroTheme.xaml");
                    themeName = "Aero";
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

        private void AeroTheme_Click(object sender, RoutedEventArgs e) => ApplyTheme("Aero");
        private void FlatTheme_Click(object sender, RoutedEventArgs e) => ApplyTheme("Flat");
        private void DarkTheme_Click(object sender, RoutedEventArgs e) => ApplyTheme("Dark");
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
