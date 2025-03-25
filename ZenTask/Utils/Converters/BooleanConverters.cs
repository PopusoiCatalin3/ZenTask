using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace ZenTask.Utils.Converters
{
    /// <summary>
    /// Converts boolean values to various UI properties
    /// </summary>
    public class BooleanConverters
    {
        [ValueConversion(typeof(bool), typeof(Visibility))]
        public class ToVisibility : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                bool boolValue = value is bool b && b;
                bool invert = parameter is string s && s.Equals("invert", StringComparison.OrdinalIgnoreCase);

                if (invert)
                    return boolValue ? Visibility.Collapsed : Visibility.Visible;

                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is Visibility visibility)
                {
                    bool isVisible = visibility == Visibility.Visible;
                    bool invert = parameter is string s && s.Equals("invert", StringComparison.OrdinalIgnoreCase);
                    return invert ? !isVisible : isVisible;
                }
                return false;
            }
        }

        [ValueConversion(typeof(bool), typeof(Brush))]
        public class ToErrorBrush : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                bool hasError = value is bool b && b;
                return hasError
                    ? new SolidColorBrush(Color.FromRgb(255, 64, 64)) // Red for error
                    : new SolidColorBrush(Color.FromRgb(221, 221, 221)); // Default border color
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        [ValueConversion(typeof(bool), typeof(Thickness))]
        public class ToThickness : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                bool hasError = value is bool b && b;
                return hasError
                    ? new Thickness(2) // Thicker for error
                    : new Thickness(1); // Default thickness
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        [ValueConversion(typeof(bool), typeof(TextDecorationCollection))]
        public class ToStrikethrough : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return (value is bool b && b) ? TextDecorations.Strikethrough : null;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        [ValueConversion(typeof(bool), typeof(DropShadowEffect))]
        public class ToDropShadow : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is bool boolValue && boolValue)
                {
                    return new DropShadowEffect
                    {
                        Color = Colors.Black,
                        Direction = 315,
                        ShadowDepth = 5,
                        BlurRadius = 10,
                        Opacity = 0.3
                    };
                }
                return null;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return value is DropShadowEffect;
            }
        }

        [ValueConversion(typeof(bool), typeof(FontWeight))]
        public class ToFontWeight : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is bool boolValue)
                {
                    bool inverted = parameter is string paramStr &&
                                   paramStr.Equals("invert", StringComparison.OrdinalIgnoreCase);

                    if (inverted)
                        boolValue = !boolValue;

                    return boolValue ? FontWeights.Normal : FontWeights.SemiBold;
                }
                return FontWeights.Normal;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}