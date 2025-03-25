using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ZenTask.Models;
using ZenTask.Services.Data;
using ZenTask.ViewModels.Base;
using Task = ZenTask.Models.Task;
using TaskStatus = ZenTask.Models.TaskStatus;
using System.Threading.Tasks;
using System.Diagnostics;
using ZenTask.Services;

namespace ZenTask.ViewModels
{
    public class KanbanViewModel : ViewModelBase
    {
        private readonly TaskRepository _taskRepository;
        private readonly CategoryRepository _categoryRepository;
        private readonly TagRepository _tagRepository;

        // Collections for tasks by status
        private ObservableCollection<Task> _todoTasks;
        private ObservableCollection<Task> _inProgressTasks;
        private ObservableCollection<Task> _completedTasks;
        private ObservableCollection<Category> _categories;

        // State properties
        private bool _isLoading;
        private string _errorMessage;
        private bool _hasError;
        private int _filterCategoryId;

        // Counts
        private int _todoCount;
        private int _inProgressCount;
        private int _completedCount;

        #region Properties

        public ObservableCollection<Task> TodoTasks
        {
            get => _todoTasks;
            set => SetProperty(ref _todoTasks, value);
        }

        public ObservableCollection<Task> InProgressTasks
        {
            get => _inProgressTasks;
            set => SetProperty(ref _inProgressTasks, value);
        }

        public ObservableCollection<Task> CompletedTasks
        {
            get => _completedTasks;
            set => SetProperty(ref _completedTasks, value);
        }

        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        private void OnIsLoadingChanged()
        {
            Debug.WriteLine($"KanbanViewModel: IsLoading changed to {IsLoading}");
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    OnIsLoadingChanged();
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                SetProperty(ref _errorMessage, value);
                HasError = !string.IsNullOrEmpty(value);

                // Log errors for debugging
                if (!string.IsNullOrEmpty(value))
                {
                    Debug.WriteLine($"KanbanViewModel ERROR: {value}");
                }
            }
        }

        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        public int FilterCategoryId
        {
            get => _filterCategoryId;
            set
            {
                if (SetProperty(ref _filterCategoryId, value))
                {
                    Debug.WriteLine($"KanbanViewModel: Filter category changed to {value}");
                    // Refresh tasks when filter changes
                    LoadTasksCommand.Execute(null);
                }
            }
        }

        public int TodoCount
        {
            get => _todoCount;
            set => SetProperty(ref _todoCount, value);
        }

        public int InProgressCount
        {
            get => _inProgressCount;
            set => SetProperty(ref _inProgressCount, value);
        }

        public int CompletedCount
        {
            get => _completedCount;
            set => SetProperty(ref _completedCount, value);
        }

        #endregion

        #region Commands

        public ICommand LoadTasksCommand { get; }
        public ICommand CreateTaskCommand { get; }
        public ICommand UpdateTaskStatusCommand { get; }
        public ICommand ClearErrorCommand { get; }

        #endregion

        public KanbanViewModel(TaskRepository taskRepository, CategoryRepository categoryRepository, TagRepository tagRepository)
        {
            Debug.WriteLine("KanbanViewModel: Constructor called");

            _taskRepository = taskRepository;
            _categoryRepository = categoryRepository;
            _tagRepository = tagRepository;

            // Initialize collections
            TodoTasks = new ObservableCollection<Task>();
            InProgressTasks = new ObservableCollection<Task>();
            CompletedTasks = new ObservableCollection<Task>();
            Categories = new ObservableCollection<Category>();

            // Initialize commands
            LoadTasksCommand = new RelayCommand(_ => LoadTasksAsyncWrapper(), _ => !IsLoading);
            UpdateTaskStatusCommand = new RelayCommand(param => UpdateTaskStatusAsyncWrapper(param as Tuple<int, TaskStatus>), _ => !IsLoading);
            ClearErrorCommand = new RelayCommand(_ => ErrorMessage = string.Empty);

            // We're reusing the CreateTaskCommand from TaskViewModel
            // This command will need to be initialized in the MainWindow.xaml.cs

            // Subscribe to task service events
            TaskServiceManager.Instance.TaskStatusChanged += TaskService_TaskStatusChanged;
            TaskServiceManager.Instance.TaskDeleted += TaskService_TaskDeleted;
            TaskServiceManager.Instance.TaskCreated += TaskService_TaskCreated;
            TaskServiceManager.Instance.TaskUpdated += TaskService_TaskUpdated;
            TaskServiceManager.Instance.TasksReloaded += TaskService_TasksReloaded;

            Debug.WriteLine("KanbanViewModel: Initialization complete");
        }
        private void TaskService_TaskStatusChanged(object sender, TaskStatusChangedEventArgs e)
        {
            Debug.WriteLine($"KanbanViewModel: Received TaskStatusChanged event - Task {e.TaskId} to {e.NewStatus}");

            try
            {
                // Find the task in our collections
                Task taskToMove = null;

                // Check in which collection the task currently exists
                switch (e.NewStatus)
                {
                    case TaskStatus.ToDo:
                        taskToMove = InProgressTasks.FirstOrDefault(t => t.Id == e.TaskId) ??
                                     CompletedTasks.FirstOrDefault(t => t.Id == e.TaskId);
                        break;

                    case TaskStatus.InProgress:
                        taskToMove = TodoTasks.FirstOrDefault(t => t.Id == e.TaskId) ??
                                     CompletedTasks.FirstOrDefault(t => t.Id == e.TaskId);
                        break;

                    case TaskStatus.Completed:
                        taskToMove = TodoTasks.FirstOrDefault(t => t.Id == e.TaskId) ??
                                     InProgressTasks.FirstOrDefault(t => t.Id == e.TaskId);
                        break;
                }

                if (taskToMove != null)
                {
                    // Remove the task from its current collection
                    if (TodoTasks.Contains(taskToMove))
                        TodoTasks.Remove(taskToMove);
                    else if (InProgressTasks.Contains(taskToMove))
                        InProgressTasks.Remove(taskToMove);
                    else if (CompletedTasks.Contains(taskToMove))
                        CompletedTasks.Remove(taskToMove);

                    // Update the task's status
                    taskToMove.Status = e.NewStatus;

                    // Add to the appropriate collection
                    switch (e.NewStatus)
                    {
                        case TaskStatus.ToDo:
                            TodoTasks.Add(taskToMove);
                            break;

                        case TaskStatus.InProgress:
                            InProgressTasks.Add(taskToMove);
                            break;

                        case TaskStatus.Completed:
                            CompletedTasks.Add(taskToMove);
                            break;
                    }

                    // Update counts
                    TodoCount = TodoTasks.Count;
                    InProgressCount = InProgressTasks.Count;
                    CompletedCount = CompletedTasks.Count;

                    Debug.WriteLine($"KanbanViewModel: Task moved successfully, counts updated - ToDo: {TodoCount}, InProgress: {InProgressCount}, Completed: {CompletedCount}");
                }
                else
                {
                    Debug.WriteLine($"KanbanViewModel: Task {e.TaskId} not found in collections, reloading all tasks");
                    LoadTasksAsyncWrapper();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"KanbanViewModel: Error handling task status change - {ex.Message}");
                // Don't show an error to the user, just refresh the data
                LoadTasksAsyncWrapper();
            }
        }

        private void TaskService_TaskDeleted(object sender, int taskId)
        {
            Debug.WriteLine($"KanbanViewModel: Received TaskDeleted event - Task {taskId}");

            try
            {
                // Remove the task from all collections
                var taskInTodo = TodoTasks.FirstOrDefault(t => t.Id == taskId);
                if (taskInTodo != null)
                    TodoTasks.Remove(taskInTodo);

                var taskInProgress = InProgressTasks.FirstOrDefault(t => t.Id == taskId);
                if (taskInProgress != null)
                    InProgressTasks.Remove(taskInProgress);

                var taskInCompleted = CompletedTasks.FirstOrDefault(t => t.Id == taskId);
                if (taskInCompleted != null)
                    CompletedTasks.Remove(taskInCompleted);

                // Update counts
                TodoCount = TodoTasks.Count;
                InProgressCount = InProgressTasks.Count;
                CompletedCount = CompletedTasks.Count;

                Debug.WriteLine($"KanbanViewModel: Task removed, counts updated - ToDo: {TodoCount}, InProgress: {InProgressCount}, Completed: {CompletedCount}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"KanbanViewModel: Error handling task deletion - {ex.Message}");
                LoadTasksAsyncWrapper();
            }
        }

        private void TaskService_TaskCreated(object sender, Task newTask)
        {
            Debug.WriteLine($"KanbanViewModel: Received TaskCreated event - Task {newTask.Id}");

            try
            {
                // Add the task to the appropriate collection based on its status
                switch (newTask.Status)
                {
                    case TaskStatus.ToDo:
                        TodoTasks.Add(newTask);
                        break;

                    case TaskStatus.InProgress:
                        InProgressTasks.Add(newTask);
                        break;

                    case TaskStatus.Completed:
                        CompletedTasks.Add(newTask);
                        break;
                }

                // Update counts
                TodoCount = TodoTasks.Count;
                InProgressCount = InProgressTasks.Count;
                CompletedCount = CompletedTasks.Count;

                Debug.WriteLine($"KanbanViewModel: Task added, counts updated - ToDo: {TodoCount}, InProgress: {InProgressCount}, Completed: {CompletedCount}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"KanbanViewModel: Error handling new task - {ex.Message}");
                LoadTasksAsyncWrapper();
            }
        }

        private void TaskService_TaskUpdated(object sender, Task updatedTask)
        {
            Debug.WriteLine($"KanbanViewModel: Received TaskUpdated event - Task {updatedTask.Id}");

            try
            {
                // Find and update the task in the appropriate collection
                var existingTask = TodoTasks.FirstOrDefault(t => t.Id == updatedTask.Id) ??
                                  InProgressTasks.FirstOrDefault(t => t.Id == updatedTask.Id) ??
                                  CompletedTasks.FirstOrDefault(t => t.Id == updatedTask.Id);

                if (existingTask != null)
                {
                    // If status changed, we need to move the task to another collection
                    if (existingTask.Status != updatedTask.Status)
                    {
                        // Remove from current collection
                        if (TodoTasks.Contains(existingTask))
                            TodoTasks.Remove(existingTask);
                        else if (InProgressTasks.Contains(existingTask))
                            InProgressTasks.Remove(existingTask);
                        else if (CompletedTasks.Contains(existingTask))
                            CompletedTasks.Remove(existingTask);

                        // Add to new collection
                        switch (updatedTask.Status)
                        {
                            case TaskStatus.ToDo:
                                TodoTasks.Add(updatedTask);
                                break;

                            case TaskStatus.InProgress:
                                InProgressTasks.Add(updatedTask);
                                break;

                            case TaskStatus.Completed:
                                CompletedTasks.Add(updatedTask);
                                break;
                        }
                    }
                    else
                    {
                        // Status didn't change, just update properties
                        var index = -1;

                        if (existingTask.Status == TaskStatus.ToDo)
                        {
                            index = TodoTasks.IndexOf(existingTask);
                            if (index >= 0)
                            {
                                TodoTasks[index] = updatedTask;
                            }
                        }
                        else if (existingTask.Status == TaskStatus.InProgress)
                        {
                            index = InProgressTasks.IndexOf(existingTask);
                            if (index >= 0)
                            {
                                InProgressTasks[index] = updatedTask;
                            }
                        }
                        else if (existingTask.Status == TaskStatus.Completed)
                        {
                            index = CompletedTasks.IndexOf(existingTask);
                            if (index >= 0)
                            {
                                CompletedTasks[index] = updatedTask;
                            }
                        }
                    }

                    // Update counts (although they shouldn't change in this case)
                    TodoCount = TodoTasks.Count;
                    InProgressCount = InProgressTasks.Count;
                    CompletedCount = CompletedTasks.Count;

                    Debug.WriteLine($"KanbanViewModel: Task updated, counts updated - ToDo: {TodoCount}, InProgress: {InProgressCount}, Completed: {CompletedCount}");
                }
                else
                {
                    Debug.WriteLine($"KanbanViewModel: Updated task not found in collections, reloading all tasks");
                    LoadTasksAsyncWrapper();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"KanbanViewModel: Error handling task update - {ex.Message}");
                LoadTasksAsyncWrapper();
            }
        }

        private void TaskService_TasksReloaded(object sender, EventArgs e)
        {
            Debug.WriteLine("KanbanViewModel: Received TasksReloaded event");
            LoadTasksAsyncWrapper();
        }

        #region Command Wrappers

        private async void LoadTasksAsyncWrapper()
        {
            Debug.WriteLine("KanbanViewModel: LoadTasksAsyncWrapper called");
            try
            {
                await LoadTasksAsync();
                Debug.WriteLine("KanbanViewModel: LoadTasksAsync completed successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"KanbanViewModel: Error in LoadTasksAsyncWrapper - {ex.Message}");
                ErrorMessage = $"Error loading tasks: {ex.Message}";
            }
        }

        private async void UpdateTaskStatusAsyncWrapper(Tuple<int, TaskStatus> parameter)
        {
            if (parameter == null)
            {
                Debug.WriteLine("KanbanViewModel: UpdateTaskStatusAsyncWrapper called with null parameter");
                return;
            }

            Debug.WriteLine($"KanbanViewModel: UpdateTaskStatusAsyncWrapper called for Task ID {parameter.Item1} to status {parameter.Item2}");
            try
            {
                await UpdateTaskStatusAsync(parameter.Item1, parameter.Item2);
                Debug.WriteLine("KanbanViewModel: UpdateTaskStatusAsync completed successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"KanbanViewModel: Error in UpdateTaskStatusAsyncWrapper - {ex.Message}");
                ErrorMessage = $"Error updating task status: {ex.Message}";
            }
        }

        #endregion

        #region Tasks Loading and Filtering

        private async System.Threading.Tasks.Task LoadTasksAsync()
        {
            if (IsLoading)
            {
                Debug.WriteLine("KanbanViewModel: LoadTasksAsync - Already loading, skipping");
                return;
            }

            try
            {
                Debug.WriteLine("KanbanViewModel: LoadTasksAsync started");
                IsLoading = true;
                ErrorMessage = string.Empty;

                // Load categories
                Debug.WriteLine("KanbanViewModel: Loading categories");
                var categories = await _categoryRepository.GetAllAsync();
                Categories.Clear();

                // Add "All Categories" option
                Categories.Add(new Category { Id = 0, Name = "Toate categoriile" });

                foreach (var category in categories)
                {
                    Categories.Add(category);
                }
                Debug.WriteLine($"KanbanViewModel: {categories.Count()} categories loaded");

                // Load all tasks
                Debug.WriteLine("KanbanViewModel: Loading all tasks");
                var allTasks = await _taskRepository.GetAllAsync();
                Debug.WriteLine($"KanbanViewModel: {allTasks.Count()} tasks loaded");

                // Apply category filter if selected
                if (FilterCategoryId > 0)
                {
                    Debug.WriteLine($"KanbanViewModel: Applying category filter {FilterCategoryId}");
                    allTasks = allTasks.Where(t => t.CategoryId == FilterCategoryId).ToList();
                    Debug.WriteLine($"KanbanViewModel: {allTasks.Count()} tasks after filtering");
                }

                // Organize tasks by status
                TodoTasks.Clear();
                InProgressTasks.Clear();
                CompletedTasks.Clear();

                foreach (var task in allTasks)
                {
                    switch (task.Status)
                    {
                        case TaskStatus.ToDo:
                            TodoTasks.Add(task);
                            break;
                        case TaskStatus.InProgress:
                            InProgressTasks.Add(task);
                            break;
                        case TaskStatus.Completed:
                            CompletedTasks.Add(task);
                            break;
                    }
                }

                // Update counts
                TodoCount = TodoTasks.Count;
                InProgressCount = InProgressTasks.Count;
                CompletedCount = CompletedTasks.Count;

                Debug.WriteLine($"KanbanViewModel: Task counts - ToDo: {TodoCount}, InProgress: {InProgressCount}, Completed: {CompletedCount}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"KanbanViewModel: Error in LoadTasksAsync - {ex.Message}");
                Debug.WriteLine($"KanbanViewModel: Stack trace - {ex.StackTrace}");
                ErrorMessage = $"Error loading tasks: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Task Status Management

        private async System.Threading.Tasks.Task UpdateTaskStatusAsync(int taskId, TaskStatus newStatus)
        {
            if (IsLoading)
            {
                Debug.WriteLine("KanbanViewModel: UpdateTaskStatusAsync - Already loading, skipping");
                return;
            }

            try
            {
                Debug.WriteLine($"KanbanViewModel: UpdateTaskStatusAsync - Task ID: {taskId}, New Status: {newStatus}");
                IsLoading = true;
                ErrorMessage = string.Empty;

                // Find task in ALL collections (improved lookup)
                Task taskToUpdate = TodoTasks.FirstOrDefault(t => t.Id == taskId)
                    ?? InProgressTasks.FirstOrDefault(t => t.Id == taskId)
                    ?? CompletedTasks.FirstOrDefault(t => t.Id == taskId);

                if (taskToUpdate != null)
                {
                    Debug.WriteLine($"KanbanViewModel: Found task to update - Title: {taskToUpdate.Title}, Current Status: {taskToUpdate.Status}");

                    // Update status in database first - this is critical
                    bool success = await _taskRepository.UpdateTaskStatusAsync(taskId, newStatus);
                    Debug.WriteLine($"KanbanViewModel: Database update result: {success}");

                    if (success)
                    {
                        // Remove from current collection
                        if (taskToUpdate.Status == TaskStatus.ToDo)
                        {
                            Debug.WriteLine("KanbanViewModel: Removing from TodoTasks");
                            TodoTasks.Remove(taskToUpdate);
                        }
                        else if (taskToUpdate.Status == TaskStatus.InProgress)
                        {
                            Debug.WriteLine("KanbanViewModel: Removing from InProgressTasks");
                            InProgressTasks.Remove(taskToUpdate);
                        }
                        else if (taskToUpdate.Status == TaskStatus.Completed)
                        {
                            Debug.WriteLine("KanbanViewModel: Removing from CompletedTasks");
                            CompletedTasks.Remove(taskToUpdate);
                        }

                        // Update task status
                        taskToUpdate.Status = newStatus;

                        // Add to new collection
                        switch (newStatus)
                        {
                            case TaskStatus.ToDo:
                                Debug.WriteLine("KanbanViewModel: Adding to TodoTasks");
                                TodoTasks.Add(taskToUpdate);
                                break;
                            case TaskStatus.InProgress:
                                Debug.WriteLine("KanbanViewModel: Adding to InProgressTasks");
                                InProgressTasks.Add(taskToUpdate);
                                break;
                            case TaskStatus.Completed:
                                Debug.WriteLine("KanbanViewModel: Adding to CompletedTasks");
                                CompletedTasks.Add(taskToUpdate);
                                break;
                        }

                        // Update counts
                        TodoCount = TodoTasks.Count;
                        InProgressCount = InProgressTasks.Count;
                        CompletedCount = CompletedTasks.Count;

                        // Notify other ViewModels about the change via the task service
                        TaskServiceManager.Instance.NotifyTaskStatusChanged(taskId, newStatus);
                    }
                    else
                    {
                        Debug.WriteLine("KanbanViewModel: Failed to update task status in database");
                        ErrorMessage = "Failed to update task status";
                    }
                }
                else
                {
                    Debug.WriteLine($"KanbanViewModel: Task with ID {taskId} not found in any collection");
                    // If task not found in collections, reload tasks
                    await LoadTasksAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"KanbanViewModel: Error in UpdateTaskStatusAsync - {ex.Message}");
                Debug.WriteLine($"KanbanViewModel: Stack trace - {ex.StackTrace}");
                ErrorMessage = $"Error updating task status: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion
    }
}