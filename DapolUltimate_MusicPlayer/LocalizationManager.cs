using System;
using System.Globalization;
using System.Windows;

namespace DapolUltimate_MusicPlayer {
    internal static class LocalizationManager {
        public static void ApplyLanguage(string cultureName) {
            try {
                var dict = new ResourceDictionary();
                var uri = new Uri($"pack://application:,,,/DapolUltimate_MusicPlayer;component/Localization/Strings.{cultureName}.xaml");
                dict.Source = uri;

                // remove previous localization dictionaries
                for (int i = 0; i < Application.Current.Resources.MergedDictionaries.Count; i++) {
                    var md = Application.Current.Resources.MergedDictionaries[i];
                    if (md.Source != null && md.Source.OriginalString.Contains("Localization/Strings.")) {
                        Application.Current.Resources.MergedDictionaries.Remove(md);
                        i--;
                    }
                }
                Application.Current.Resources.MergedDictionaries.Add(dict);

                Properties.Settings.Default.SelectedLanguage = cultureName;
                Properties.Settings.Default.Save();

                CultureInfo.CurrentUICulture = new CultureInfo(cultureName);
            }
            catch {
                // ignore errors
            }
        }
    }
}
