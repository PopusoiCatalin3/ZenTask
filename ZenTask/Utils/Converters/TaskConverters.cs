using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ZenTask.Models;
using ZenTask.ViewModels;
using TaskStatus = ZenTask.Models.TaskStatus;
using Task = ZenTask.Models.Task;
using System.Windows.Controls;
using System.Windows.Media.Effects;

namespace ZenTask.Utils.Converters
{
    /// <summary>
    /// Convertește TaskPriority la Brush pentru indicatoare vizuale
    /// </summary>
    public class PriorityToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TaskPriority priority && Application.Current?.Resources != null)
            {
                object resource;
                Color color;

                switch (priority)
                {
                    case TaskPriority.Low:
                        resource = Application.Current.Resources["GreenColor"];
                        color = resource is Color ? (Color)resource : Colors.Green;
                        return new SolidColorBrush(color);

                    case TaskPriority.Medium:
                        resource = Application.Current.Resources["TealColor"];
                        color = resource is Color ? (Color)resource : Colors.Teal;
                        return new SolidColorBrush(color);

                    case TaskPriority.High:
                        resource = Application.Current.Resources["PurpleColor"];
                        color = resource is Color ? (Color)resource : Colors.Purple;
                        return new SolidColorBrush(color);

                    case TaskPriority.Urgent:
                        resource = Application.Current.Resources["ErrorColor"];
                        color = resource is Color ? (Color)resource : Colors.Red;
                        return new SolidColorBrush(color);

                    default:
                        return new SolidColorBrush(Colors.Teal);
                }
            }

            return new SolidColorBrush((Color)Application.Current.Resources["TealColor"]);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertește TaskStatus la Brush pentru indicatoare vizuale
    /// </summary>
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TaskStatus status)
            {
                try
                {
                    switch (status)
                    {
                        case TaskStatus.ToDo:
                            if (Application.Current.Resources.Contains("PeachColor"))
                                return new SolidColorBrush((Color)Application.Current.Resources["PeachColor"]);
                            return new SolidColorBrush(Colors.Orange); // Fallback color for ToDo

                        case TaskStatus.InProgress:
                            if (Application.Current.Resources.Contains("PurpleColor"))
                                return new SolidColorBrush((Color)Application.Current.Resources["PurpleColor"]);
                            return new SolidColorBrush(Colors.Purple); // Fallback color for InProgress

                        case TaskStatus.Completed:
                            if (Application.Current.Resources.Contains("GreenColor"))
                                return new SolidColorBrush((Color)Application.Current.Resources["GreenColor"]);
                            return new SolidColorBrush(Colors.Green); // Fallback color for Completed

                        default:
                            return new SolidColorBrush(Colors.Orange); // Default fallback
                    }
                }
                catch (Exception)
                {
                    // In case of any exception, return a fallback brush
                    return new SolidColorBrush(Colors.Gray);
                }
            }

            return new SolidColorBrush(Colors.Gray); // Default fallback
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertește TaskStatus la text
    /// </summary>
    public class StatusToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TaskStatus status)
            {
                return status switch
                {
                    TaskStatus.ToDo => "De făcut",
                    TaskStatus.InProgress => "În progres",
                    TaskStatus.Completed => "Finalizat",
                    _ => "Necunoscut"
                };
            }

            return "Necunoscut";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertește un bool către stare de tăiere text (pentru sarcini completate)
    /// </summary>
    public class BoolToStrikethroughConverter : IValueConverter
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

    /// <summary>
    /// Convertește un string gol la Visibility
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
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

    /// <summary>
    /// Convertește un obiect null la Visibility
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
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

    /// <summary>
    /// Convertește un număr la Visibility (dacă e mai mare ca 0)
    /// </summary>
    public class NumberToVisibilityConverter : IValueConverter
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

    /// <summary>
    /// Convertește StatusTask la vizibilitate (pentru a ascunde butonul de complete pentru task-uri deja completate)
    /// </summary>
    public class StatusToVisibilityConverter : IValueConverter
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

    /// <summary>
    /// Convertește IsEditMode la text pentru titlul panoului
    /// </summary>
    public class EditModeTitleConverter : IValueConverter
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

    /// <summary>
    /// Convertește IsEditMode la text pentru butonul de acțiune
    /// </summary>
    public class EditModeButtonConverter : IValueConverter
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

    /// <summary>
    /// Convertește IsEditMode la comanda corespunzătoare
    /// </summary>
    public class EditModeCommandConverter : IValueConverter
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

    /// <summary>
    /// Convertește TagId la brush folosind Tag-urile din TaskViewModel
    /// </summary>
    public class TagIdToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int tagId)
            {
                var userControl = parameter as UserControl;
                var viewModel = userControl?.DataContext as TaskViewModel;
                var tag = viewModel?.Tags.FirstOrDefault(t => t.Id == tagId);

                if (tag != null && !string.IsNullOrEmpty(tag.ColorHex))
                {
                    try
                    {
                        var color = ColorConverter.ConvertFromString(tag.ColorHex);
                        return new SolidColorBrush((Color)color);
                    }
                    catch { }
                }
            }

            return new SolidColorBrush((Color)Application.Current.Resources["PurpleColor"]);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertește TagId la nume folosind Tag-urile din TaskViewModel
    /// </summary>
    public class TagIdToNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int tagId)
            {
                var userControl = parameter as UserControl;
                var viewModel = userControl?.DataContext as TaskViewModel;
                var tag = viewModel?.Tags.FirstOrDefault(t => t.Id == tagId);

                if (tag != null)
                {
                    return tag.Name;
                }
            }

            return "Tag necunoscut";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Verifică dacă un TagId este selectat pentru task-ul curent
    /// </summary>
    public class TagIdToIsSelectedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int tagId)
            {
                var userControl = parameter as UserControl;
                var viewModel = userControl?.DataContext as TaskViewModel;

                if (viewModel?.TaskTagIds != null)
                {
                    return viewModel.TaskTagIds.Contains(tagId);
                }
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class StringToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorStr && !string.IsNullOrEmpty(colorStr))
            {
                try
                {
                    // Presupunem că string-ul este în format hex (ex: "#FF4081")
                    Color color = (Color)ColorConverter.ConvertFromString(colorStr);
                    return new SolidColorBrush(color);
                }
                catch
                {
                    // În caz de eroare returnăm o culoare implicită
                    return new SolidColorBrush(Colors.Gray);
                }
            }

            // Valoare implicită
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                return brush.Color.ToString();
            }

            return null;
        }
    }
    public class BoolToDropShadowConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue)
            {
                // Create a drop shadow effect when the boolean is true
                return new DropShadowEffect
                {
                    Color = System.Windows.Media.Colors.Black,
                    Direction = 315,
                    ShadowDepth = 5,
                    BlurRadius = 10,
                    Opacity = 0.3
                };
            }

            // Return null (no effect) when the boolean is false
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Typically not needed for effects, but for completeness:
            return value is DropShadowEffect ? true : false;
        }
    }
    public class PriorityToIsCheckedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TaskPriority priority && parameter is string priorityParam)
            {
                return priorityParam switch
                {
                    "Low" => priority == TaskPriority.Low,
                    "Medium" => priority == TaskPriority.Medium,
                    "High" => priority == TaskPriority.High,
                    "Urgent" => priority == TaskPriority.Urgent,
                    _ => false
                };
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked && parameter is string priorityParam)
            {
                return priorityParam switch
                {
                    "Low" => TaskPriority.Low,
                    "Medium" => TaskPriority.Medium,
                    "High" => TaskPriority.High,
                    "Urgent" => TaskPriority.Urgent,
                    _ => TaskPriority.Medium
                };
            }
            return TaskPriority.Medium;
        }
    }
    public class StatusZeroConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TaskStatus status)
            {
                // Butonul "Toate" este activ când status este diferit de ToDo sau când este ToDo dar avem un filtru activ
                return (int)status != 0;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertește TaskStatus pentru activarea butonului ToDo
    /// </summary>
    public class StatusToDoActiveConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TaskStatus status)
            {
                return status != TaskStatus.ToDo;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertește TaskStatus pentru activarea butonului InProgress
    /// </summary>
    public class StatusInProgressActiveConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TaskStatus status)
            {
                return status != TaskStatus.InProgress;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertește TaskStatus pentru activarea butonului Completed
    /// </summary>
    public class StatusCompletedActiveConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TaskStatus status)
            {
                return status != TaskStatus.Completed;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}