using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using ZenTask.Models;
using TaskStatus = ZenTask.Models.TaskStatus;

namespace ZenTask.Utils.Converters
{
    /// <summary>
    /// Converters for handling UI element visibility
    /// </summary>
    public class VisibilityConverters
    {
        [ValueConversion(typeof(string), typeof(Visibility))]
        public class FromString : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                bool invert = parameter != null && parameter.ToString() == "invert";
                bool isEmpty = string.IsNullOrEmpty(value as string);

                return invert ?
                    (isEmpty ? Visibility.Visible : Visibility.Collapsed) :
                    (isEmpty ? Visibility.Collapsed : Visibility.Visible);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        [ValueConversion(typeof(object), typeof(Visibility))]
        public class FromNull : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                bool invert = parameter != null && parameter.ToString() == "invert";
                bool isNull = value == null;

                return invert ?
                    (isNull ? Visibility.Visible : Visibility.Collapsed) :
                    (isNull ? Visibility.Collapsed : Visibility.Visible);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        [ValueConversion(typeof(int), typeof(Visibility))]
        public class FromNumber : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value == null) return Visibility.Collapsed;

                bool invert = parameter != null && parameter.ToString() == "invert";
                bool isGreaterThanZero = false;

                if (value is int intValue)
                    isGreaterThanZero = intValue > 0;
                else if (value is double doubleValue)
                    isGreaterThanZero = doubleValue > 0;
                else if (value is decimal decimalValue)
                    isGreaterThanZero = decimalValue > 0;

                return invert ?
                    (isGreaterThanZero ? Visibility.Collapsed : Visibility.Visible) :
                    (isGreaterThanZero ? Visibility.Visible : Visibility.Collapsed);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        [ValueConversion(typeof(TaskStatus), typeof(Visibility))]
        public class FromTaskStatus : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is TaskStatus status)
                {
                    return status != TaskStatus.Completed ? Visibility.Visible : Visibility.Collapsed;
                }
                return Visibility.Visible;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
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
}