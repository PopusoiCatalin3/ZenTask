using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace ZenTask.Utils.Converters
{
    [MarkupExtensionReturnType(typeof(IValueConverter))]
    public class BoolToVisibilityExtension : MarkupExtension, IValueConverter
    {
        public bool Invert { get; set; } = false;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = value is bool b && b;

            if (parameter is string strParam && strParam.Equals("invert", StringComparison.OrdinalIgnoreCase))
            {
                Invert = true;
            }

            if (Invert)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }

            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                bool isVisible = visibility == Visibility.Visible;
                return Invert ? !isVisible : isVisible;
            }

            return false;
        }
    }
}
