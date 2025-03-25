using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using ZenTask.Models;
using ZenTask.ViewModels;
using TaskStatus = ZenTask.Models.TaskStatus;

namespace ZenTask.Utils.Converters
{
    /// <summary>
    /// Converters related to application entities (Task, Category, Tag)
    /// </summary>
    public class EntityConverters
    {
        [ValueConversion(typeof(int), typeof(Brush))]
        public class CategoryColorToBrush : IValueConverter
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
                return new SolidColorBrush((Color)Application.Current.Resources["NavyColor"]);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        [ValueConversion(typeof(int), typeof(string))]
        public class CategoryIdToName : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is int categoryId && categoryId > 0 && Application.Current?.MainWindow != null)
                {
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

        [ValueConversion(typeof(string), typeof(Brush))]
        public class ColorHexToBrush : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is string colorStr && !string.IsNullOrEmpty(colorStr))
                {
                    try
                    {
                        Color color = (Color)ColorConverter.ConvertFromString(colorStr);
                        return new SolidColorBrush(color);
                    }
                    catch
                    {
                        return new SolidColorBrush(Colors.Gray);
                    }
                }
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

        [ValueConversion(typeof(int), typeof(Brush))]
        public class TagIdToBrush : IValueConverter
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

        [ValueConversion(typeof(int), typeof(string))]
        public class TagIdToName : IValueConverter
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

        [ValueConversion(typeof(int), typeof(bool))]
        public class TagIdToIsSelected : IValueConverter
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

        [ValueConversion(typeof(object[]), typeof(Brush))]
        public class TaskBorderBrush : IMultiValueConverter
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
    }
}