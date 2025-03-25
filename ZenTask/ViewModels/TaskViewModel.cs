using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ZenTask.Models;
using ZenTask.Services.Data;
using ZenTask.ViewModels.Base;

using Task = ZenTask.Models.Task;
using TaskStatus = ZenTask.Models.TaskStatus;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ZenTask.Views;
using ZenTask.Services;
using System.Diagnostics;

namespace ZenTask.ViewModels
{
    public class TaskViewModel : ViewModelBase
    {
        private readonly TaskRepository _taskRepository;
        private readonly CategoryRepository _categoryRepository;
        private readonly TagRepository _tagRepository;

        private ObservableCollection<Task> _tasks;
        private ObservableCollection<Category> _categories;
        private ObservableCollection<Tag> _tags;

        private Task _selectedTask;
        private Category _selectedCategory;
        private bool _isLoading;
        private string _errorMessage;
        private bool _hasError;

        private string _searchText;
        private TaskStatus _filterStatus;
        private int _filterCategoryId;
        private bool _showCompletedTasks;

        private string _taskTitle;
        private string _taskDescription;
        private DateTime? _taskDueDate;
        private TaskPriority _taskPriority;
        private int _taskCategoryId;
        private ObservableCollection<SubTask> _taskSubTasks;
        private ObservableCollection<int> _taskTagIds;
        private bool _isEditMode;
        private bool _isPopupOpen;
        private bool _isEditingTask;
        private string _titleError;
        private bool _hasTitleError;

        #region Properties

        public bool IsPopupOpen
        {
            get => _isPopupOpen;
            set => SetProperty(ref _isPopupOpen, value);
        }

        public bool IsEditingTask
        {
            get => _isEditingTask;
            set => SetProperty(ref _isEditingTask, value);
        }

        public string TitleError
        {
            get => _titleError;
            set => SetProperty(ref _titleError, value);
        }

        public bool HasTitleError
        {
            get => _hasTitleError;
            set => SetProperty(ref _hasTitleError, value);
        }

        public ObservableCollection<Task> Tasks
        {
            get => _tasks;
            set => SetProperty(ref _tasks, value);
        }

        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        public ObservableCollection<Tag> Tags
        {
            get => _tags;
            set => SetProperty(ref _tags, value);
        }

        public Task SelectedTask
        {
            get => _selectedTask;
            set
            {
                if (SetProperty(ref _selectedTask, value) && value != null)
                {
                    // Populează proprietățile de editare când se selectează un task
                    TaskTitle = value.Title;
                    TaskDescription = value.Description;
                    TaskDueDate = value.DueDate;
                    TaskPriority = value.Priority;
                    TaskCategoryId = value.CategoryId;

                    TaskSubTasks = new ObservableCollection<SubTask>(value.SubTasks);
                    TaskTagIds = new ObservableCollection<int>(value.TagIds);

                    IsEditMode = true;
                }
            }
        }

        public Category SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                SetProperty(ref _errorMessage, value);
                HasError = !string.IsNullOrEmpty(value);
            }
        }

        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public TaskStatus FilterStatus
        {
            get => _filterStatus;
            set => SetProperty(ref _filterStatus, value);
        }

        public int FilterCategoryId
        {
            get => _filterCategoryId;
            set => SetProperty(ref _filterCategoryId, value);
        }

        public bool ShowCompletedTasks
        {
            get => _showCompletedTasks;
            set => SetProperty(ref _showCompletedTasks, value);
        }

        public string TaskTitle
        {
            get => _taskTitle;
            set => SetProperty(ref _taskTitle, value);
        }

        public string TaskDescription
        {
            get => _taskDescription;
            set => SetProperty(ref _taskDescription, value);
        }

        public DateTime? TaskDueDate
        {
            get => _taskDueDate;
            set => SetProperty(ref _taskDueDate, value);
        }

        public TaskPriority TaskPriority
        {
            get => _taskPriority;
            set => SetProperty(ref _taskPriority, value);
        }

        public int TaskCategoryId
        {
            get => _taskCategoryId;
            set => SetProperty(ref _taskCategoryId, value);
        }

        public ObservableCollection<SubTask> TaskSubTasks
        {
            get => _taskSubTasks;
            set => SetProperty(ref _taskSubTasks, value);
        }

        public ObservableCollection<int> TaskTagIds
        {
            get => _taskTagIds;
            set => SetProperty(ref _taskTagIds, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        #endregion

        #region Commands

        public ICommand LoadTasksCommand { get; }
        public ICommand CreateTaskCommand { get; }
        public ICommand UpdateTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand CompleteTaskCommand { get; }
        public ICommand FilterByStatusCommand { get; }
        public ICommand FilterByCategoryCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand AddSubTaskCommand { get; }
        public ICommand RemoveSubTaskCommand { get; }
        public ICommand ToggleSubTaskCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand AddTagToTaskCommand { get; }
        public ICommand RemoveTagFromTaskCommand { get; }
        public ICommand ClearErrorCommand { get; }
        public ICommand EditTaskCommand { get; }
        public ICommand ToggleTagCommand { get; }
        public ICommand ClosePopupCommand { get; }
        public ICommand SaveTaskCommand { get; }

        #endregion

        // Constructor
        public TaskViewModel(TaskRepository taskRepository, CategoryRepository categoryRepository, TagRepository tagRepository)
        {
            _taskRepository = taskRepository;
            _categoryRepository = categoryRepository;
            _tagRepository = tagRepository;

            // Inițializăm colecțiile
            Tasks = new ObservableCollection<Task>();
            Categories = new ObservableCollection<Category>();
            Tags = new ObservableCollection<Tag>();
            TaskSubTasks = new ObservableCollection<SubTask>();
            TaskTagIds = new ObservableCollection<int>();

            // Inițializăm comenzile
            LoadTasksCommand = new RelayCommand(_ => LoadTasksAsyncWrapper(), _ => !IsLoading);
            CreateTaskCommand = new RelayCommand(_ => OpenCreateTaskPopup(), _ => !IsLoading);
            UpdateTaskCommand = new RelayCommand(_ => UpdateTaskAsyncWrapper(), _ => !IsLoading && IsEditMode && !string.IsNullOrWhiteSpace(TaskTitle));
            DeleteTaskCommand = new RelayCommand(param => DeleteTaskAsyncWrapper(param is int id ? id : SelectedTask?.Id ?? 0), _ => (SelectedTask != null || _ is int) && !IsLoading);
            CompleteTaskCommand = new RelayCommand(param => CompleteTaskAsyncWrapper(param is int id ? id : SelectedTask?.Id ?? 0), _ => !IsLoading);
            FilterByStatusCommand = new RelayCommand(param => FilterByStatusWrapper((TaskStatus)param), _ => !IsLoading);
            FilterByCategoryCommand = new RelayCommand(param => FilterByCategoryWrapper((int)param), _ => !IsLoading);
            ClearFiltersCommand = new RelayCommand(_ => ClearFiltersWrapper(), _ => !IsLoading);
            AddSubTaskCommand = new RelayCommand(_ => AddSubTask(), _ => !IsLoading);
            RemoveSubTaskCommand = new RelayCommand(param => RemoveSubTask((int)param), _ => !IsLoading);
            ToggleSubTaskCommand = new RelayCommand(param => ToggleSubTaskAsyncWrapper((int)param), _ => !IsLoading);
            SearchCommand = new RelayCommand(_ => SearchTasksWrapper(), _ => !IsLoading);
            CancelEditCommand = new RelayCommand(_ => CancelEdit(), _ => IsEditMode);
            AddTagToTaskCommand = new RelayCommand(param => AddTagToTask((int)param), _ => !IsLoading);
            RemoveTagFromTaskCommand = new RelayCommand(param => RemoveTagFromTask((int)param), _ => !IsLoading);
            ClearErrorCommand = new RelayCommand(_ => ErrorMessage = string.Empty);
            EditTaskCommand = new RelayCommand(param => EditTask(param as Task), _ => !IsLoading);
            ToggleTagCommand = new RelayCommand(param => ToggleTag((int)param), _ => !IsLoading);

            ClosePopupCommand = new RelayCommand(_ => ClosePopup());
            SaveTaskCommand = new RelayCommand(_ => SaveTaskFromPopupAsync());

            TaskServiceManager.Instance.TaskStatusChanged += TaskService_TaskStatusChanged;
            TaskServiceManager.Instance.TaskDeleted += TaskService_TaskDeleted;
            TaskServiceManager.Instance.TaskCreated += TaskService_TaskCreated;
            TaskServiceManager.Instance.TaskUpdated += TaskService_TaskUpdated;
            TaskServiceManager.Instance.TasksReloaded += TaskService_TasksReloaded;

            FilterStatus = TaskStatus.ToDo;
            ShowCompletedTasks = false;
            SearchText = string.Empty;

            ResetTaskForm();
        }

        private void TaskService_TaskStatusChanged(object sender, TaskStatusChangedEventArgs e)
        {
            Debug.WriteLine($"TaskViewModel: Received task status changed notification for task {e.TaskId} to {e.NewStatus}");

            // Update the task in our local collection
            var task = Tasks.FirstOrDefault(t => t.Id == e.TaskId);
            if (task != null)
            {
                task.Status = e.NewStatus;

                // If we have tasks filtered by status, refresh the view
                if (FilterStatus != TaskStatus.ToDo || !ShowCompletedTasks)
                {
                    LoadTasksAsyncWrapper();
                }
            }
        }

        private void TaskService_TaskDeleted(object sender, int taskId)
        {
            Debug.WriteLine($"TaskViewModel: Received task deleted notification for task {taskId}");

            // Remove the task from our collection if it exists
            var task = Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                Tasks.Remove(task);
            }

            // If the deleted task was the selected task, clear the selection
            if (SelectedTask?.Id == taskId)
            {
                SelectedTask = null;
                ResetTaskForm();
                IsEditMode = false;
            }
        }

        private void TaskService_TaskCreated(object sender, Models.Task newTask)
        {
            Debug.WriteLine($"TaskViewModel: Received task created notification for task {newTask.Id}");

            // Only add the task to our collection if it matches our current filter
            bool shouldAdd = true;

            // Check status filter
            if (FilterStatus != TaskStatus.ToDo)
            {
                shouldAdd = newTask.Status == FilterStatus;
            }
            else if (!ShowCompletedTasks && newTask.Status == TaskStatus.Completed)
            {
                shouldAdd = false;
            }

            // Check category filter
            if (shouldAdd && FilterCategoryId > 0)
            {
                shouldAdd = newTask.CategoryId == FilterCategoryId;
            }

            // Check search filter
            if (shouldAdd && !string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                shouldAdd = newTask.Title.ToLower().Contains(searchLower) ||
                           (newTask.Description != null && newTask.Description.ToLower().Contains(searchLower));
            }

            if (shouldAdd)
            {
                // Add to collection but make sure we don't add duplicates
                if (!Tasks.Any(t => t.Id == newTask.Id))
                {
                    Tasks.Add(newTask);
                }
            }
        }

        private void TaskService_TaskUpdated(object sender, Models.Task updatedTask)
        {
            Debug.WriteLine($"TaskViewModel: Received task updated notification for task {updatedTask.Id}");

            // Find the task in our collection
            var existingTask = Tasks.FirstOrDefault(t => t.Id == updatedTask.Id);
            if (existingTask != null)
            {
                // Check if the updated task still matches our filter criteria
                bool shouldKeep = true;

                // Check status filter
                if (FilterStatus != TaskStatus.ToDo)
                {
                    shouldKeep = updatedTask.Status == FilterStatus;
                }
                else if (!ShowCompletedTasks && updatedTask.Status == TaskStatus.Completed)
                {
                    shouldKeep = false;
                }

                // Check category filter
                if (shouldKeep && FilterCategoryId > 0)
                {
                    shouldKeep = updatedTask.CategoryId == FilterCategoryId;
                }

                // Check search filter
                if (shouldKeep && !string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    shouldKeep = updatedTask.Title.ToLower().Contains(searchLower) ||
                               (updatedTask.Description != null && updatedTask.Description.ToLower().Contains(searchLower));
                }

                if (shouldKeep)
                {
                    // Replace the task with the updated version
                    int index = Tasks.IndexOf(existingTask);
                    Tasks[index] = updatedTask;

                    // If this was the selected task, update the selection
                    if (SelectedTask?.Id == updatedTask.Id)
                    {
                        SelectedTask = updatedTask;
                    }
                }
                else
                {
                    // Remove the task as it no longer matches our filter
                    Tasks.Remove(existingTask);

                    // If this was the selected task, clear the selection
                    if (SelectedTask?.Id == updatedTask.Id)
                    {
                        SelectedTask = null;
                        ResetTaskForm();
                        IsEditMode = false;
                    }
                }
            }
            else
            {
                // Task wasn't in our collection, but check if it should be based on filter
                bool shouldAdd = true;

                // Apply the same filter checks as above
                if (FilterStatus != TaskStatus.ToDo)
                {
                    shouldAdd = updatedTask.Status == FilterStatus;
                }
                else if (!ShowCompletedTasks && updatedTask.Status == TaskStatus.Completed)
                {
                    shouldAdd = false;
                }

                if (shouldAdd && FilterCategoryId > 0)
                {
                    shouldAdd = updatedTask.CategoryId == FilterCategoryId;
                }

                if (shouldAdd && !string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    shouldAdd = updatedTask.Title.ToLower().Contains(searchLower) ||
                               (updatedTask.Description != null && updatedTask.Description.ToLower().Contains(searchLower));
                }

                if (shouldAdd)
                {
                    Tasks.Add(updatedTask);
                }
            }
        }

        private void TaskService_TasksReloaded(object sender, EventArgs e)
        {
            Debug.WriteLine("TaskViewModel: Received tasks reloaded notification");
            LoadTasksAsyncWrapper();
        }

        #region Wrapper Methods for async

        private async void LoadTasksAsyncWrapper()
        {
            try
            {
                await LoadTasksAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading tasks: {ex.Message}";
            }
        }

        private async void CreateTaskAsyncWrapper()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                // Create task in database
                await CreateTaskAsync();

                // Show success notification
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.ShowSuccessNotification(
                    "Task Created",
                    $"New task '{TaskTitle}' has been created successfully.");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error creating task: {ex.Message}";

                // Show error notification
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.ShowErrorNotification(
                    "Error",
                    $"Could not create task: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }


        private async void DeleteTaskAsyncWrapper(int taskId)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                // Find task before deleting it (so we have the name for the notification)
                var task = Tasks.FirstOrDefault(t => t.Id == taskId);
                string taskTitle = task?.Title ?? "Task";

                // Delete from database - don't try to capture a return value
                await DeleteTaskAsync(taskId);

                // Show success notification
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.ShowSuccessNotification(
                    "Task Deleted",
                    $"'{taskTitle}' has been deleted successfully.");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error deleting task: {ex.Message}";

                // Show error notification
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.ShowErrorNotification(
                    "Error",
                    $"Could not delete task: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void UpdateTaskAsyncWrapper()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                // Update task in database - don't try to capture a return value
                await UpdateTaskAsync();

                // Show success notification
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.ShowSuccessNotification(
                    "Task Updated",
                    $"The task has been updated successfully.",
                    2000); // Short duration
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error updating task: {ex.Message}";

                // Show error notification
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.ShowErrorNotification(
                    "Error",
                    $"Could not update task: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void CompleteTaskAsyncWrapper(int taskId)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                // Get reference to the main window for notifications
                var mainWindow = Application.Current.MainWindow as MainWindow;

                // Find the task in our collection
                var task = Tasks.FirstOrDefault(t => t.Id == taskId);

                if (task != null)
                {
                    // Update status in database
                    var success = await _taskRepository.UpdateTaskStatusAsync(taskId, Models.TaskStatus.Completed);

                    if (success)
                    {
                        // Show success notification
                        mainWindow?.ShowSuccessNotification(
                            "Task Completed",
                            $"'{task.Title}' has been marked as completed.");

                        // Refresh tasks list
                        await RefreshTasksAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error completing task: {ex.Message}";

                // Show error notification
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.ShowErrorNotification(
                    "Error",
                    $"Could not complete task: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void FilterByStatusWrapper(TaskStatus status)
        {
            try
            {
                await FilterByStatusAsync(status);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error filtering by status: {ex.Message}";
            }
        }

        private async void FilterByCategoryWrapper(int categoryId)
        {
            try
            {
                await FilterByCategoryAsync(categoryId);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error filtering by category: {ex.Message}";
            }
        }

        private async void ClearFiltersWrapper()
        {
            try
            {
                await ClearFiltersAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error clearing filters: {ex.Message}";
            }
        }

        private async void SearchTasksWrapper()
        {
            try
            {
                await SearchTasksAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error searching tasks: {ex.Message}";
            }
        }

        private async void ToggleSubTaskAsyncWrapper(int subTaskId)
        {
            try
            {
                await ToggleSubTaskAsync(subTaskId);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error toggling subtask: {ex.Message}";
            }
        }

        #endregion

        #region Metode pentru încărcarea datelor

        public async System.Threading.Tasks.Task LoadTasksAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                // Încărcăm categoriile
                var categories = await _categoryRepository.GetAllAsync();
                Categories.Clear();
                foreach (var category in categories)
                {
                    Categories.Add(category);
                }

                // Încărcăm tag-urile
                var tags = await _tagRepository.GetAllAsync();
                Tags.Clear();
                foreach (var tag in tags)
                {
                    Tags.Add(tag);
                }

                // Încărcăm sarcinile
                await RefreshTasksAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Eroare la încărcarea datelor: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async System.Threading.Tasks.Task RefreshTasksAsync()
        {
            // Încărcăm toate sarcinile
            var allTasks = await _taskRepository.GetAllAsync();

            // Filtrăm sarcinile conform criteriilor actuale
            var filteredTasks = FilterTasks(allTasks);

            // Actualizăm colecția observabilă
            Tasks.Clear();
            foreach (var task in filteredTasks)
            {
                Tasks.Add(task);
            }
        }

        #endregion

        #region Metode pentru filtrarea sarcinilor

        private IEnumerable<Task> FilterTasks(IEnumerable<Task> tasks)
        {
            var filteredTasks = tasks;

            // Filtrare după status
            if (FilterStatus != TaskStatus.ToDo || !ShowCompletedTasks)
            {
                if (FilterStatus == TaskStatus.ToDo && !ShowCompletedTasks)
                {
                    // Dacă filtrăm după ToDo și nu arătăm sarcinile finalizate
                    filteredTasks = filteredTasks.Where(t => t.Status != TaskStatus.Completed);
                }
                else
                {
                    // Filtrare specifică după status
                    filteredTasks = filteredTasks.Where(t => t.Status == FilterStatus);
                }
            }

            // Filtrare după categorie
            if (FilterCategoryId > 0)
            {
                filteredTasks = filteredTasks.Where(t => t.CategoryId == FilterCategoryId);
            }

            // Filtrare după text de căutare
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filteredTasks = filteredTasks.Where(t =>
                    t.Title.ToLower().Contains(searchLower) ||
                    (t.Description != null && t.Description.ToLower().Contains(searchLower)));
            }

            // Sortăm după prioritate și dată scadentă
            filteredTasks = filteredTasks
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.DueDate ?? DateTime.MaxValue);

            return filteredTasks;
        }

        private async System.Threading.Tasks.Task FilterByStatusAsync(TaskStatus status)
        {
            FilterStatus = status;
            await RefreshTasksAsync();
        }

        private async System.Threading.Tasks.Task FilterByCategoryAsync(int categoryId)
        {
            FilterCategoryId = categoryId;
            await RefreshTasksAsync();
        }

        private async System.Threading.Tasks.Task ClearFiltersAsync()
        {
            FilterStatus = TaskStatus.ToDo;
            FilterCategoryId = 0;
            SearchText = string.Empty;
            ShowCompletedTasks = false;
            await RefreshTasksAsync();
        }

        private async System.Threading.Tasks.Task SearchTasksAsync()
        {
            await RefreshTasksAsync();
        }

        #endregion

        #region Metode pentru gestionarea sarcinilor

        private void ResetTaskForm()
        {
            TaskTitle = string.Empty;
            TaskDescription = string.Empty;
            TaskDueDate = null;
            TaskPriority = TaskPriority.Medium;

            // Only set default category if we have categories loaded
            if (Categories != null && Categories.Count > 0)
            {
                TaskCategoryId = Categories[0].Id;
            }
            else
            {
                TaskCategoryId = 0;
            }

            // Create new collections to ensure references are fresh
            TaskSubTasks = new ObservableCollection<SubTask>();
            TaskTagIds = new ObservableCollection<int>();
        }

        private void CancelEdit()
        {
            ResetTaskForm();
            SelectedTask = null;
        }

        //private void EditTask(Task task)
        //{
        //    if (task != null)
        //    {
        //        SelectedTask = task;
        //    }
        //}

        private bool ValidateTaskTitle()
        {
            // Check if title is empty or contains only whitespace
            if (string.IsNullOrWhiteSpace(TaskTitle))
            {
                TitleError = "Titlu nu poate fi gol";
                HasTitleError = true;
                return false;
            }

            // Clear error state
            TitleError = string.Empty;
            HasTitleError = false;
            return true;
        }
        private void EditTask(Task task)
        {
            if (task != null)
            {
                OpenEditTaskPopup(task);
            }
        }
        private async System.Threading.Tasks.Task CreateTaskAsync()
        {
            if (string.IsNullOrWhiteSpace(TaskTitle)) return;

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                // Create a new Task object with the form data
                var newTask = new Task
                {
                    Title = TaskTitle,
                    Description = TaskDescription,
                    CreatedDate = DateTime.Now,
                    DueDate = TaskDueDate,
                    Priority = TaskPriority,
                    Status = TaskStatus.ToDo,
                    CategoryId = TaskCategoryId,
                    SubTasks = TaskSubTasks.ToList(),
                    TagIds = TaskTagIds.ToList()
                };

                // Save to the database
                var taskId = await _taskRepository.InsertAsync(newTask);
                if (taskId > 0)
                {
                    newTask.Id = taskId;

                    // Notify other ViewModels about the new task
                    TaskServiceManager.Instance.NotifyTaskCreated(newTask);

                    // Refresh our task list
                    await RefreshTasksAsync();
                    ResetTaskForm();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error creating task: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async System.Threading.Tasks.Task UpdateTaskAsync()
        {
            if (SelectedTask == null || string.IsNullOrWhiteSpace(TaskTitle)) return;

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                // Actualizăm task-ul cu datele din formular
                SelectedTask.Title = TaskTitle;
                SelectedTask.Description = TaskDescription;
                SelectedTask.DueDate = TaskDueDate;
                SelectedTask.Priority = TaskPriority;
                SelectedTask.CategoryId = TaskCategoryId;
                SelectedTask.SubTasks = TaskSubTasks.ToList();
                SelectedTask.TagIds = TaskTagIds.ToList();

                // Salvăm în baza de date
                var success = await _taskRepository.UpdateAsync(SelectedTask);
                if (success)
                {
                    // Reîmprospătăm lista de task-uri
                    await RefreshTasksAsync();
                    ResetTaskForm();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Eroare la actualizarea sarcinii: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async System.Threading.Tasks.Task DeleteTaskAsync(int taskId)
        {
            if (taskId <= 0) return;

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                // Delete from database
                var success = await _taskRepository.DeleteAsync(taskId);
                if (success)
                {
                    // Notify other ViewModels about the deletion
                    TaskServiceManager.Instance.NotifyTaskDeleted(taskId);

                    // If the deleted task is the selected task, reset the form
                    if (SelectedTask?.Id == taskId)
                    {
                        ResetTaskForm();
                    }

                    // Refresh our task list
                    await RefreshTasksAsync();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error deleting task: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async System.Threading.Tasks.Task CompleteTaskAsync(int taskId)
        {
            if (taskId <= 0) return;

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                // Update status in database
                var success = await _taskRepository.UpdateTaskStatusAsync(taskId, TaskStatus.Completed);
                if (success)
                {
                    // Notify other ViewModels about the status change
                    TaskServiceManager.Instance.NotifyTaskStatusChanged(taskId, TaskStatus.Completed);

                    // Refresh our task list
                    await RefreshTasksAsync();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error completing task: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Metode pentru gestionarea sub-sarcinilor

        private void AddSubTask()
        {
            if (TaskSubTasks == null)
            {
                TaskSubTasks = new ObservableCollection<SubTask>();
            }

            // Adăugăm o sub-sarcină nouă
            var newSubTask = new SubTask
            {
                Title = "Sub-sarcină nouă",
                IsCompleted = false,
                DisplayOrder = TaskSubTasks.Count
            };

            TaskSubTasks.Add(newSubTask);
        }

        private void RemoveSubTask(int index)
        {
            if (TaskSubTasks != null && index >= 0 && index < TaskSubTasks.Count)
            {
                TaskSubTasks.RemoveAt(index);

                // Actualizăm ordinea de afișare
                for (int i = 0; i < TaskSubTasks.Count; i++)
                {
                    TaskSubTasks[i].DisplayOrder = i;
                }
            }
        }

        private async System.Threading.Tasks.Task ToggleSubTaskAsync(int subTaskId)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                // Găsim sub-sarcina în lista curentă (pentru UI)
                var subTask = TaskSubTasks?.FirstOrDefault(s => s.Id == subTaskId);
                if (subTask != null)
                {
                    // Inversăm starea de completare
                    subTask.IsCompleted = !subTask.IsCompleted;

                    // Actualizăm în baza de date dacă sub-sarcina are un ID valid
                    if (subTaskId > 0)
                    {
                        var success = await _taskRepository.UpdateSubTaskCompletionAsync(subTaskId, subTask.IsCompleted);
                        if (!success)
                        {
                            // Dacă actualizarea eșuează, revenim la starea anterioară
                            subTask.IsCompleted = !subTask.IsCompleted;
                            ErrorMessage = "Eroare la actualizarea sub-sarcinii";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Eroare la actualizarea sub-sarcinii: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Metode pentru gestionarea etichetelor

        private void AddTagToTask(int tagId)
        {
            if (!TaskTagIds.Contains(tagId))
            {
                TaskTagIds.Add(tagId);
            }
        }

        private void RemoveTagFromTask(int tagId)
        {
            if (TaskTagIds.Contains(tagId))
            {
                TaskTagIds.Remove(tagId);
            }
        }

        private void ToggleTag(int tagId)
        {
            if (TaskTagIds.Contains(tagId))
            {
                TaskTagIds.Remove(tagId);
            }
            else
            {
                TaskTagIds.Add(tagId);
            }
        }

        #endregion

        #region Metode pentru gestionarea popup-ului

        // Metodă pentru deschiderea popup-ului pentru crearea unui task nou
        private async void OpenCreateTaskPopup()
        {
            try
            {
                // Check if Categories and Tags are loaded - if not, load them
                if ((Categories == null || Categories.Count == 0) ||
                    (Tags == null || Tags.Count == 0))
                {
                    IsLoading = true;

                    try
                    {
                        // Load categories if needed
                        if (Categories == null || Categories.Count == 0)
                        {
                            var categories = await _categoryRepository.GetAllAsync();
                            Categories = new System.Collections.ObjectModel.ObservableCollection<Models.Category>(categories);
                        }

                        // Load tags if needed
                        if (Tags == null || Tags.Count == 0)
                        {
                            var tags = await _tagRepository.GetAllAsync();
                            Tags = new System.Collections.ObjectModel.ObservableCollection<Models.Tag>(tags);
                        }
                    }
                    finally
                    {
                        IsLoading = false;
                    }
                }

                // Ensure there's at least one category for the form
                if (Categories.Count > 0 && TaskCategoryId <= 0)
                {
                    TaskCategoryId = Categories[0].Id;
                }

                // Reset the form for a new task
                ResetTaskForm();

                // Set state for creating a new task
                IsEditingTask = false;
                IsPopupOpen = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OpenCreateTaskPopup: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                // Show error notification to user
                var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
                mainWindow?.ShowErrorNotification(
                    "Error",
                    $"Could not open task form: {ex.Message}");
            }
        }

        /// <summary>
        /// Opens the edit task popup with data from an existing task
        /// </summary>
        private void OpenEditTaskPopup(Models.Task task)
        {
            if (task == null) return;

            try
            {
                // Populate form with task details
                TaskTitle = task.Title;
                TaskDescription = task.Description;
                TaskDueDate = task.DueDate;
                TaskPriority = task.Priority;
                TaskCategoryId = task.CategoryId;

                // Clone subtasks to avoid modifying originals directly
                TaskSubTasks = new System.Collections.ObjectModel.ObservableCollection<Models.SubTask>();
                foreach (var subTask in task.SubTasks)
                {
                    TaskSubTasks.Add(new Models.SubTask
                    {
                        Id = subTask.Id,
                        TaskId = subTask.TaskId,
                        Title = subTask.Title,
                        IsCompleted = subTask.IsCompleted,
                        DisplayOrder = subTask.DisplayOrder
                    });
                }

                // Clone tag IDs
                TaskTagIds = new System.Collections.ObjectModel.ObservableCollection<int>(task.TagIds);

                // Set selected task and mode
                SelectedTask = task;
                IsEditingTask = true;
                IsEditMode = true;
                IsPopupOpen = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OpenEditTaskPopup: {ex.Message}");

                // Show error notification to user
                var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
                mainWindow?.ShowErrorNotification(
                    "Error",
                    $"Could not open task for editing: {ex.Message}");
            }
        }

        /// <summary>
        /// Closes the popup
        /// </summary>
        private void ClosePopup()
        {
            // First set the flag to false to trigger closing
            IsPopupOpen = false;

            // Reset editing state
            IsEditingTask = false;
            IsEditMode = false;

            // Reset the form to clear any temporary data
            ResetTaskForm();

            // Clear selected task
            SelectedTask = null;
        }

        /// <summary>
        /// Creates a new default drop shadow effect
        /// </summary>
        private static System.Windows.Media.Effects.DropShadowEffect CreateDefaultShadowEffect()
        {
            return new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = System.Windows.Media.Colors.Black,
                Direction = 315,
                ShadowDepth = 5,
                BlurRadius = 10,
                Opacity = 0.3
            };
        }

        // Metodă pentru salvarea unui task (folosită de popup)
        private async System.Threading.Tasks.Task SaveTaskFromPopupAsync()
        {
            // Add validation check - prevent empty title
            if (string.IsNullOrWhiteSpace(TaskTitle))
            {
                // Set error state
                TitleError = "Titlu nu poate fi gol";
                HasTitleError = true;

                // Focus the title input to guide the user
                Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                    var popup = Application.Current.MainWindow?.FindName("TaskPopupWrapper") as TaskPopupWrapper;
                    var titleTextBox = popup?.PopupView?.FindName("TitleTextBox") as TextBox;
                    titleTextBox?.Focus();
                }));

                // Show error notification
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.ShowErrorNotification(
                    "Validare eșuată",
                    "Titlu nu poate fi gol. Vă rugăm completați un titlu pentru sarcină.");

                return; // Stop execution
            }
            else
            {
                // Clear any previous error state
                TitleError = string.Empty;
                HasTitleError = false;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                if (IsEditingTask)
                {
                    // Actualizăm task-ul existent
                    if (SelectedTask != null)
                    {
                        SelectedTask.Title = TaskTitle;
                        SelectedTask.Description = TaskDescription;
                        SelectedTask.DueDate = TaskDueDate;
                        SelectedTask.Priority = TaskPriority;
                        SelectedTask.CategoryId = TaskCategoryId;
                        SelectedTask.SubTasks = TaskSubTasks.ToList();
                        SelectedTask.TagIds = TaskTagIds.ToList();
                        // Salvăm în baza de date
                        var success = await _taskRepository.UpdateAsync(SelectedTask);
                        if (success)
                        {
                            // Show success notification
                            var mainWindow = Application.Current.MainWindow as MainWindow;
                            mainWindow?.ShowSuccessNotification(
                                "Actualizare reușită",
                                $"Sarcina '{TaskTitle}' a fost actualizată.");

                            // Reîmprospătăm lista de task-uri
                            await RefreshTasksAsync();
                            ResetTaskForm();
                            ClosePopup();
                        }
                    }
                }
                else
                {
                    // Creăm un task nou
                    var newTask = new Task
                    {
                        Title = TaskTitle,
                        Description = TaskDescription,
                        CreatedDate = DateTime.Now,
                        DueDate = TaskDueDate,
                        Priority = TaskPriority,
                        Status = TaskStatus.ToDo,
                        CategoryId = TaskCategoryId,
                        SubTasks = TaskSubTasks.ToList(),
                        TagIds = TaskTagIds.ToList()
                    };
                    // Salvăm în baza de date
                    var taskId = await _taskRepository.InsertAsync(newTask);
                    if (taskId > 0)
                    {
                        // Show success notification
                        var mainWindow = Application.Current.MainWindow as MainWindow;
                        mainWindow?.ShowSuccessNotification(
                            "Adăugare reușită",
                            $"Sarcina nouă '{TaskTitle}' a fost creată.");

                        newTask.Id = taskId;
                        // Reîmprospătăm lista de task-uri
                        await RefreshTasksAsync();
                        ResetTaskForm();
                        ClosePopup();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = IsEditingTask ?
                    $"Eroare la actualizarea sarcinii: {ex.Message}" :
                    $"Eroare la crearea sarcinii: {ex.Message}";

                // Show error notification
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.ShowErrorNotification(
                    "Eroare",
                    ErrorMessage);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion



    }
}