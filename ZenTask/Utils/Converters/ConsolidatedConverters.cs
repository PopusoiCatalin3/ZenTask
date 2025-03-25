using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;
using ZenTask.Models;
using ZenTask.ViewModels;
using TaskStatus = ZenTask.Models.TaskStatus;

namespace ZenTask.Utils.Converters
{
    /// <summary>
    /// Consolidated converter class for boolean-based conversions
    /// </summary>
    public class BooleanConverter : IValueConverter
    {
        public enum ConversionType
        {
            Visibility,
            Brush,
            Thickness,
            Strikethrough,
            DropShadow,
            FontWeight,
            ErrorBrush
        }

        public ConversionType Type { get; set; }
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = value is bool b && b;

            // Check for inversion parameter
            if (parameter is string paramStr && paramStr.Equals("invert", StringComparison.OrdinalIgnoreCase))
                boolValue = !boolValue;

            // Apply configured inversion
            if (Invert)
                boolValue = !boolValue;

            switch (Type)
            {
                case ConversionType.Visibility:
                    return boolValue ? Visibility.Visible : Visibility.Collapsed;

                case ConversionType.Brush:
                    return boolValue
                        ? new SolidColorBrush(Colors.Green)
                        : new SolidColorBrush(Colors.Red);

                case ConversionType.Thickness:
                    return boolValue
                        ? new Thickness(2)
                        : new Thickness(1);

                case ConversionType.Strikethrough:
                    return boolValue ? TextDecorations.Strikethrough : null;

                case ConversionType.DropShadow:
                    if (boolValue)
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

                case ConversionType.FontWeight:
                    return boolValue ? FontWeights.Normal : FontWeights.SemiBold;

                case ConversionType.ErrorBrush:
                    return boolValue
                        ? new SolidColorBrush(Color.FromRgb(255, 64, 64)) // Red for error
                        : new SolidColorBrush(Color.FromRgb(221, 221, 221)); // Default border color

                default:
                    return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Type == ConversionType.Visibility && value is Visibility visibility)
            {
                bool isVisible = visibility == Visibility.Visible;
                return Invert ? !isVisible : isVisible;
            }

            return false;
        }
    }

    /// <summary>
    /// Consolidated converter for EntityType conversions (Task, Category, Tag)
    /// </summary>
    public class EntityConverter : IValueConverter
    {
        public enum ConversionType
        {
            TaskStatusToBrush,
            TaskStatusToString,
            PriorityToBrush,
            CategoryColorToBrush,
            TagIdToBrush,
            TagIdToName,
            TagIdToIsSelected
        }

        public ConversionType Type { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (Type)
            {
                case ConversionType.TaskStatusToBrush:
                    if (value is TaskStatus status)
                    {
                        try
                        {
                            switch (status)
                            {
                                case TaskStatus.ToDo:
                                    if (Application.Current.Resources.Contains("PeachColor"))
                                        return new SolidColorBrush((Color)Application.Current.Resources["PeachColor"]);
                                    return new SolidColorBrush(Colors.Orange);

                                case TaskStatus.InProgress:
                                    if (Application.Current.Resources.Contains("PurpleColor"))
                                        return new SolidColorBrush((Color)Application.Current.Resources["PurpleColor"]);
                                    return new SolidColorBrush(Colors.Purple);

                                case TaskStatus.Completed:
                                    if (Application.Current.Resources.Contains("GreenColor"))
                                        return new SolidColorBrush((Color)Application.Current.Resources["GreenColor"]);
                                    return new SolidColorBrush(Colors.Green);
                            }
                        }
                        catch
                        {
                            return new SolidColorBrush(Colors.Gray);
                        }
                    }
                    return new SolidColorBrush(Colors.Gray);

                case ConversionType.TaskStatusToString:
                    if (value is TaskStatus statusStr)
                    {
                        return statusStr switch
                        {
                            TaskStatus.ToDo => "De făcut",
                            TaskStatus.InProgress => "În progres",
                            TaskStatus.Completed => "Finalizat",
                            _ => "Necunoscut"
                        };
                    }
                    return "Necunoscut";

                case ConversionType.PriorityToBrush:
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
                        }
                    }
                    return new SolidColorBrush(Colors.Teal);

                case ConversionType.CategoryColorToBrush:
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

                case ConversionType.TagIdToBrush:
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

                case ConversionType.TagIdToName:
                    if (value is int tagIdName)
                    {
                        var userControl = parameter as UserControl;
                        var viewModel = userControl?.DataContext as TaskViewModel;
                        var tag = viewModel?.Tags.FirstOrDefault(t => t.Id == tagIdName);

                        if (tag != null)
                        {
                            return tag.Name;
                        }
                    }
                    return "Tag necunoscut";

                case ConversionType.TagIdToIsSelected:
                    if (value is int tagIdSelected)
                    {
                        var userControl = parameter as UserControl;
                        var viewModel = userControl?.DataContext as TaskViewModel;

                        if (viewModel?.TaskTagIds != null)
                        {
                            return viewModel.TaskTagIds.Contains(tagIdSelected);
                        }
                    }
                    return false;

                default:
                    return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Extended markup for BooleanConverter to allow for XAML usage
    /// </summary>
    public class BoolToVisibilityExtension : System.Windows.Markup.MarkupExtension, IValueConverter
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

    /// <summary>
    /// Multi-value converter for task properties
    /// </summary>
    public class TaskMultiConverter : IMultiValueConverter
    {
        public enum ConversionType
        {
            TaskBorderBrush
        }

        public ConversionType Type { get; set; }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (Type == ConversionType.TaskBorderBrush)
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
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}