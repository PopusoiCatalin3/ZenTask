using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ZenTask.ViewModels;

namespace ZenTask.Utils.Converters
{
    /// <summary>
    /// Converters related to UI state (edit mode, selection, etc.)
    /// </summary>
    public class UIStateConverters
    {
        [ValueConversion(typeof(bool), typeof(string))]
        public class EditModeTitle : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return (value is bool isEdit && isEdit) ? "Editează sarcina" : "Adaugă sarcină nouă";
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        [ValueConversion(typeof(bool), typeof(string))]
        public class EditModeButton : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return (value is bool isEdit && isEdit) ? "Salvează" : "Adaugă";
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        [ValueConversion(typeof(bool), typeof(System.Windows.Input.ICommand))]
        public class EditModeCommand : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is bool isEdit && targetType == typeof(System.Windows.Input.ICommand))
                {
                    var vm = (parameter as FrameworkElement)?.DataContext as TaskViewModel;
                    return isEdit ? vm?.UpdateTaskCommand : vm?.CreateTaskCommand;
                }
                return null;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}