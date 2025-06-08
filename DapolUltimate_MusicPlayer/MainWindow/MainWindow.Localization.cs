using System.Windows;
using System.Windows.Controls;

namespace DapolUltimate_MusicPlayer {
    public partial class MainWindow {
        private void LanguageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (LanguageSelector.SelectedIndex == 0) {
                LocalizationManager.ApplyLanguage("en-US");
            }
            else if (LanguageSelector.SelectedIndex == 1) {
                LocalizationManager.ApplyLanguage("ru-RU");
            }
            else if (LanguageSelector.SelectedIndex == 2) {
                LocalizationManager.ApplyLanguage("pl-PL");
            }
        }
    }
}
