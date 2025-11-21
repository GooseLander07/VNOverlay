using System;
using System.Globalization;
using System.Windows.Data; // Critical for IValueConverter

namespace OverlayApp
{
    // This class must be Public so the XAML can see it
    public class IndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Takes the index (0, 1, 2) and adds 1 to make it (1, 2, 3)
            if (value is int i) return (i + 1).ToString();
            return "1";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}