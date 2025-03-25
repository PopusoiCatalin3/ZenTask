using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ZenTask.Models;
using TaskStatus = ZenTask.Models.TaskStatus;

namespace ZenTask.Utils.Converters
{
    /// <summary>
    /// Converter to get category color by category ID
    /// </summary>
    public class CategoryColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int categoryId && categoryId > 0 && Application.Current?.MainWindow != null)
            {
                // Try to find the category from the MainWindow's DataContext
                var mainWindow = Application.Current.MainWindow;

                if (mainWindow.FindName("MainTaskView") is FrameworkElement taskView &&
                    taskView.DataContext is ViewModels.TaskViewModel viewModel)
                {
                    var category = viewModel.Categories.FirstOrDefault(c => c.Id == categoryId);
                    if (category != null && !string.IsNullOrEmpty(category.ColorHex))
                    {
                        try
                        {
                            var color = (Color)ColorConverter.ConvertFromString(category.ColorHex);
                            return new SolidColorBrush(color);
                        }
                        catch
                        {
                            // Fallback
                        }
                    }
                }

                // Fallback colors by ID if category not found
                switch (categoryId % 5)
                {
                    case 0: return new SolidColorBrush((Color)Application.Current.Resources["WorkColor"]);
                    case 1: return new SolidColorBrush((Color)Application.Current.Resources["PersonalColor"]);
                    case 2: return new SolidColorBrush((Color)Application.Current.Resources["StudyColor"]);
                    case 3: return new SolidColorBrush((Color)Application.Current.Resources["HealthColor"]);
                    case 4: return new SolidColorBrush((Color)Application.Current.Resources["FinanceColor"]);
                }
            }

            // Default color
            return new SolidColorBrush((Color)Application.Current.Resources["NavyColor"]);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter to get category name by category ID
    /// </summary>
    public class CategoryNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int categoryId && categoryId > 0 && Application.Current?.MainWindow != null)
            {
                // Try to find the category from the MainWindow's DataContext
                var mainWindow = Application.Current.MainWindow;

                if (mainWindow.FindName("MainTaskView") is FrameworkElement taskView &&
                    taskView.DataContext is ViewModels.TaskViewModel viewModel)
                {
                    var category = viewModel.Categories.FirstOrDefault(c => c.Id == categoryId);
                    if (category != null)
                    {
                        return category.Name;
                    }
                }
            }

            return "Categorie";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for task border brush combining priority and status
    /// </summary>
    public class TaskBorderBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is TaskPriority priority && values[1] is TaskStatus status)
            {
                // Get the priority brush for the left border
                Brush priorityBrush;

                switch (priority)
                {
                    case TaskPriority.Urgent:
                        priorityBrush = new SolidColorBrush((Color)Application.Current.Resources["ErrorColor"]);
                        break;
                    case TaskPriority.High:
                        priorityBrush = new SolidColorBrush((Color)Application.Current.Resources["PurpleColor"]);
                        break;
                    case TaskPriority.Medium:
                        priorityBrush = new SolidColorBrush((Color)Application.Current.Resources["TealColor"]);
                        break;
                    case TaskPriority.Low:
                        priorityBrush = new SolidColorBrush((Color)Application.Current.Resources["GreenColor"]);
                        break;
                    default:
                        priorityBrush = new SolidColorBrush((Color)Application.Current.Resources["TealColor"]);
                        break;
                }

                // Create a custom brush for the border
                var borderBrush = new SolidColorBrush((Color)Application.Current.Resources["BorderColor"]);

                if (status == TaskStatus.Completed)
                {
                    // For completed tasks, use a more muted color
                    var completedColor = (Color)Application.Current.Resources["GreenColor"];
                    return new SolidColorBrush(Color.FromArgb(150, completedColor.R, completedColor.G, completedColor.B));
                }

                return priorityBrush;
            }

            return new SolidColorBrush((Color)Application.Current.Resources["BorderColor"]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToFontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // If parameter is "invert", invert the boolean logic
                if (parameter is string paramStr && paramStr.Equals("invert", StringComparison.OrdinalIgnoreCase))
                {
                    boolValue = !boolValue;
                }

                // If disabled (false), use SemiBold, otherwise Normal
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