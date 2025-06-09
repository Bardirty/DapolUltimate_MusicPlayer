using System;
using System.Collections.Generic;
using System.Windows;

namespace DapolUltimate_MusicPlayer {
    internal static class ThemeHelper {
        public static void ApplyTheme(ICollection<ResourceDictionary> resources, string themeName) {
            resources.Clear();

            var baseDict = new ResourceDictionary {
                Source = new Uri("pack://application:,,,/DapolUltimate_MusicPlayer;component/BaseStyles.xaml")
            };
            resources.Add(baseDict);

            var themeDict = new ResourceDictionary();
            var themePath = $"Themes/{themeName}Theme.xaml";
            var uri = new Uri($"pack://application:,,,/DapolUltimate_MusicPlayer;component/{themePath}");
            try {
                themeDict.Source = uri;
            } catch {
                themeDict.Source = new Uri("pack://application:,,,/DapolUltimate_MusicPlayer;component/Themes/AeroTheme.xaml");
            }
            resources.Add(themeDict);
        }
    }
}
