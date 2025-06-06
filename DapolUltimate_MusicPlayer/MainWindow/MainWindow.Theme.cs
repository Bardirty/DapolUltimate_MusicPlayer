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

        private void AeroTheme_Click(object sender, RoutedEventArgs e) => ApplyTheme("Aero");
        private void FlatTheme_Click(object sender, RoutedEventArgs e) => ApplyTheme("Flat");
        private void DarkTheme_Click(object sender, RoutedEventArgs e) => ApplyTheme("Dark");
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
