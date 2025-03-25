using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ZenTask.Models;
using ZenTask.ViewModels;
using Task = ZenTask.Models.Task;
using TaskStatus = ZenTask.Models.TaskStatus;

namespace ZenTask.Views
{
    /// <summary>
    /// Interaction logic for KanbanView.xaml
    /// </summary>
    public partial class KanbanView : UserControl
    {
        private Point _startPoint;
        private bool _isDragging;
        private Border _draggedElement;
        private Border _originalColumn;
        private Storyboard _currentStoryboard;
        private KanbanViewModel _viewModel;

        public KanbanView()
        {
            InitializeComponent();
            DataContextChanged += KanbanView_DataContextChanged;
            Loaded += KanbanView_Loaded;
        }

        private void KanbanView_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("KanbanView: Loaded event triggered");
            EnsureLoadingIndicatorWorks();

            if (_viewModel != null)
            {
                Debug.WriteLine("KanbanView: LoadTasksCommand execution requested from Loaded event");
                _viewModel.LoadTasksCommand.Execute(null);
            }
            else
            {
                Debug.WriteLine("KanbanView: WARNING - ViewModel is null in Loaded event");
            }
        }

        private void KanbanView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Debug.WriteLine("KanbanView: DataContext changed");
            _viewModel = DataContext as KanbanViewModel;

            if (_viewModel != null)
            {
                Debug.WriteLine("KanbanView: ViewModel set successfully");

                // Ensure loading indicator works
                EnsureLoadingIndicatorWorks();

                // Only load if view is already loaded to avoid duplicate loading
                if (IsLoaded)
                {
                    Debug.WriteLine("KanbanView: LoadTasksCommand execution requested from DataContextChanged");
                    _viewModel.LoadTasksCommand.Execute(null);
                }

                // Subscribe to error message changes to make them more visible
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
            else
            {
                Debug.WriteLine("KanbanView: WARNING - Failed to set ViewModel from DataContext");
            }
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ErrorMessage" && !string.IsNullOrEmpty(_viewModel.ErrorMessage))
            {
                Debug.WriteLine($"KanbanView: Error detected - {_viewModel.ErrorMessage}");
            }

            // Reset loading indicator when IsLoading changes
            if (e.PropertyName == "IsLoading" && !_viewModel.IsLoading)
            {
                var loadingBorder = FindName("LoadingBorder") as Border;
                if (loadingBorder != null && loadingBorder.RenderTransform is ScaleTransform transform)
                {
                    // Reset scale transform
                    transform.ScaleX = 0;
                    transform.ScaleY = 0;
                }
            }
        }

        // Event handlers for drag and drop
        private void TaskCard_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("KanbanView: PreviewMouseLeftButtonDown on task card");

            // Store the mouse position
            _startPoint = e.GetPosition(null);

            // Set the sender as the dragged element
            _draggedElement = sender as Border;

            if (_draggedElement != null)
            {
                // Store the original parent column
                _originalColumn = FindParent<Border>(_draggedElement);
                Debug.WriteLine($"KanbanView: Drag initiated on task with ID: {_draggedElement.Tag}");
                e.Handled = true;
            }
            else
            {
                Debug.WriteLine("KanbanView: WARNING - Failed to identify dragged element");
            }
        }

        private void TaskCard_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !_isDragging && _draggedElement != null)
            {
                Point position = e.GetPosition(null);

                // Calculate the distance moved
                if (Math.Abs(position.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(position.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    Debug.WriteLine("KanbanView: Starting drag operation");
                    StartDrag();
                    e.Handled = true;
                }
            }
        }

        private void TaskCard_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("KanbanView: PreviewMouseLeftButtonUp - Resetting drag operation");
            // Reset drag operation
            _draggedElement = null;
            _originalColumn = null;
            e.Handled = true;
        }

        private void Column_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(int)))
            {
                // Highlight the target column
                Border column = sender as Border;
                if (column != null)
                {
                    Debug.WriteLine($"KanbanView: DragEnter - Column with tag: {column.Tag}");

                    // Stop any current animation
                    if (_currentStoryboard != null)
                    {
                        _currentStoryboard.Stop();
                    }

                    // Change background to highlight brush
                    column.Background = FindResource("ColumnHighlightBrush") as SolidColorBrush;

                    // Apply column highlight animation
                    Storyboard highlightAnimation = FindResource("ColumnHighlightAnimation") as Storyboard;
                    if (highlightAnimation != null)
                    {
                        // Create a clone of the animation to avoid issues with reuse
                        Storyboard animationClone = highlightAnimation.Clone();

                        // Set the target
                        foreach (var timeline in animationClone.Children)
                        {
                            Storyboard.SetTarget(timeline, column);
                        }

                        _currentStoryboard = animationClone;
                        animationClone.Begin();
                        Debug.WriteLine("KanbanView: Started column highlight animation");
                    }
                    else
                    {
                        Debug.WriteLine("KanbanView: WARNING - Could not find ColumnHighlightAnimation resource");
                    }

                    e.Handled = true;
                }
            }
        }

        private void Column_DragLeave(object sender, DragEventArgs e)
        {
            // Restore column style
            Border column = sender as Border;
            if (column != null)
            {
                Debug.WriteLine($"KanbanView: DragLeave - Column with tag: {column.Tag}");

                // Stop any current animation
                if (_currentStoryboard != null)
                {
                    _currentStoryboard.Stop();
                }

                // Change background back to normal brush
                column.Background = FindResource("ColumnNormalBrush") as SolidColorBrush;

                // Apply normal animation
                Storyboard normalAnimation = FindResource("ColumnNormalAnimation") as Storyboard;
                if (normalAnimation != null)
                {
                    // Create a clone of the animation to avoid issues with reuse
                    Storyboard animationClone = normalAnimation.Clone();

                    // Set the target
                    foreach (var timeline in animationClone.Children)
                    {
                        Storyboard.SetTarget(timeline, column);
                    }

                    _currentStoryboard = animationClone;
                    animationClone.Begin();
                    Debug.WriteLine("KanbanView: Started column normal animation");
                }
                else
                {
                    Debug.WriteLine("KanbanView: WARNING - Could not find ColumnNormalAnimation resource");
                }
            }
        }

        private void Column_Drop(object sender, DragEventArgs e)
        {
            Debug.WriteLine("KanbanView: Drop event fired");

            try
            {
                if (e.Data.GetDataPresent(typeof(int)))
                {
                    // Get target column
                    Border targetColumn = sender as Border;

                    if (targetColumn != null && targetColumn.Tag != null)
                    {
                        // Get task ID
                        int taskId = (int)e.Data.GetData(typeof(int));
                        Debug.WriteLine($"KanbanView: Drop - Task ID: {taskId}");

                        // Get target status
                        TaskStatus targetStatus = (TaskStatus)targetColumn.Tag;
                        Debug.WriteLine($"KanbanView: Drop - Target status: {targetStatus}");

                        // Execute command to update task status
                        if (_viewModel != null && _viewModel.UpdateTaskStatusCommand != null &&
                            _viewModel.UpdateTaskStatusCommand.CanExecute(new Tuple<int, TaskStatus>(taskId, targetStatus)))
                        {
                            Debug.WriteLine("KanbanView: Executing UpdateTaskStatusCommand");
                            _viewModel.UpdateTaskStatusCommand.Execute(new Tuple<int, TaskStatus>(taskId, targetStatus));
                        }
                        else
                        {
                            Debug.WriteLine("KanbanView: WARNING - Cannot execute UpdateTaskStatusCommand");

                            // Try to catch specific failure reasons for better error reporting
                            if (_viewModel == null)
                            {
                                Debug.WriteLine("KanbanView: ViewModel is null");
                                ShowError("Error updating task", "Application state error - please reload the view.");
                            }
                            else if (_viewModel.UpdateTaskStatusCommand == null)
                            {
                                Debug.WriteLine("KanbanView: UpdateTaskStatusCommand is null");
                                ShowError("Error updating task", "Command not available - please reload the application.");
                            }
                            else
                            {
                                ShowError("Error updating task", "Could not execute update command - the task may have been modified elsewhere.");
                            }
                        }

                        // Reset column style
                        Column_DragLeave(sender, e);
                    }
                    else
                    {
                        Debug.WriteLine("KanbanView: WARNING - Target column or tag is null");
                        ShowError("Error updating task", "Invalid drop target - please try again.");
                    }
                }
                else
                {
                    Debug.WriteLine("KanbanView: WARNING - Data does not contain an integer task ID");
                    ShowError("Error updating task", "Invalid task data - please try again.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"KanbanView: ERROR in Column_Drop - {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
                ShowError("Error moving task", $"An unexpected error occurred: {ex.Message}");
            }
            finally
            {
                // Reset dragging state
                _isDragging = false;
                _draggedElement = null;
                _originalColumn = null;
            }
        }

        private void ShowError(string title, string message)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.ShowErrorNotification(title, message);
            }
            else
            {
                if (_viewModel != null)
                {
                    _viewModel.ErrorMessage = message;
                }
            }
        }

        private void StartDrag()
        {
            if (_draggedElement == null || _isDragging) return;

            _isDragging = true;

            try
            {
                int taskId = (int)_draggedElement.Tag;
                Debug.WriteLine($"KanbanView: StartDrag for task ID: {taskId}");

                DataObject data = new DataObject(typeof(int), taskId);

                Storyboard dragStartAnimation = FindResource("CardDragStartAnimation") as Storyboard;
                if (dragStartAnimation != null)
                {
                    Storyboard animationClone = dragStartAnimation.Clone();

                    foreach (var timeline in animationClone.Children)
                    {
                        Storyboard.SetTarget(timeline, _draggedElement);
                    }

                    animationClone.Begin();
                    Debug.WriteLine("KanbanView: Started card drag animation");
                }
                else
                {
                    Debug.WriteLine("KanbanView: WARNING - Could not find CardDragStartAnimation resource");
                }

                Debug.WriteLine("KanbanView: Starting DoDragDrop operation");
                DragDrop.DoDragDrop(_draggedElement, data, DragDropEffects.Move);
                Debug.WriteLine("KanbanView: DoDragDrop operation completed");

                Storyboard dragEndAnimation = FindResource("CardDragEndAnimation") as Storyboard;
                if (dragEndAnimation != null)
                {
                    Storyboard animationClone = dragEndAnimation.Clone();

                    foreach (var timeline in animationClone.Children)
                    {
                        Storyboard.SetTarget(timeline, _draggedElement);
                    }

                    animationClone.Begin();
                    Debug.WriteLine("KanbanView: Started card end drag animation");
                }
                else
                {
                    Debug.WriteLine("KanbanView: WARNING - Could not find CardDragEndAnimation resource");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"KanbanView: ERROR in StartDrag - {ex.Message}");
                if (_viewModel != null)
                {
                    _viewModel.ErrorMessage = $"Error during drag operation: {ex.Message}";
                }
            }
        }

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObj = VisualTreeHelper.GetParent(child);

            if (parentObj == null) return null;

            T parent = parentObj as T;
            if (parent != null)
                return parent;
            else
                return FindParent<T>(parentObj);
        }

        private void EnsureLoadingIndicatorWorks()
        {
            Grid loadingGrid = FindName("LoadingGrid") as Grid;
            if (loadingGrid == null) return;

            Panel.SetZIndex(loadingGrid, 1000);

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == "IsLoading")
                    {
                        Debug.WriteLine($"LoadingGrid visibility changing to: {_viewModel.IsLoading}");
                        loadingGrid.Visibility = _viewModel.IsLoading ? Visibility.Visible : Visibility.Collapsed;
                    }
                };
            }
        }






    }
}