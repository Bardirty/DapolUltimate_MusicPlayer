using System;
using System.Globalization;
using System.Windows.Data;

namespace DapolUltimate_MusicPlayer {
    public class MultiplyValueConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is double doubleValue && parameter is string paramStr && double.TryParse(paramStr, out double factor)) {
                return doubleValue * factor;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    public class ValueToWidthConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is double doubleValue && parameter is double maxValue && maxValue > 0) {
                return doubleValue / maxValue;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}