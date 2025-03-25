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
    /// Converters related to Task and TaskStatus
    /// </summary>
    public class TaskConverters
    {
        [ValueConversion(typeof(TaskPriority), typeof(Brush))]
        public class PriorityToBrush : IValueConverter
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
                    }
                }
                return new SolidColorBrush(Colors.Teal);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        [ValueConversion(typeof(TaskStatus), typeof(Brush))]
        public class StatusToBrush : IValueConverter
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
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        [ValueConversion(typeof(TaskStatus), typeof(string))]
        public class StatusToString : IValueConverter
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

        [ValueConversion(typeof(TaskPriority), typeof(bool))]
        public class PriorityToIsChecked : IValueConverter
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

        [ValueConversion(typeof(TaskStatus), typeof(bool))]
        public class StatusFilters : IValueConverter
        {
            private readonly Func<TaskStatus, bool> _filterFunction;

            public StatusFilters(Func<TaskStatus, bool> filterFunction)
            {
                _filterFunction = filterFunction;
            }

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is TaskStatus status)
                {
                    return _filterFunction(status);
                }
                return true;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        // Factory methods for common status filter converters
        public static StatusFilters CreateStatusZeroConverter() =>
            new StatusFilters(status => (int)status != 0);

        public static StatusFilters CreateStatusToDoConverter() =>
            new StatusFilters(status => status != TaskStatus.ToDo);

        public static StatusFilters CreateStatusInProgressConverter() =>
            new StatusFilters(status => status != TaskStatus.InProgress);

        public static StatusFilters CreateStatusCompletedConverter() =>
            new StatusFilters(status => status != TaskStatus.Completed);
    }
}