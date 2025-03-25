// Add these converters to the Utils.Converters namespace
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ZenTask.Utils.Converters
{
    /// <summary>
    /// Converts a boolean to an error brush color
    /// </summary>
    public class BoolToErrorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool hasError = value is bool b && b;

            // Use error color if there's an error, otherwise use default border color
            return hasError
                ? new SolidColorBrush(Color.FromRgb(255, 64, 64)) // Red for error
                : new SolidColorBrush(Color.FromRgb(221, 221, 221)); // Default border color
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a boolean to border thickness
    /// </summary>
    public class BoolToThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool hasError = value is bool b && b;

            // Use thicker border if there's an error
            return hasError
                ? new Thickness(2) // Thicker for error
                : new Thickness(1); // Default thickness
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}