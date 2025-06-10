using System;
using System.Windows;

namespace DapolUltimate_MusicPlayer {
    public partial class MainWindow {
        private void ApplyTheme(string themeName) {
            try {
                ThemeHelper.ApplyTheme(Resources.MergedDictionaries, themeName);

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
