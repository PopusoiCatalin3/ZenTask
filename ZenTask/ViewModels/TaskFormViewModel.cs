using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ZenTask.Models;
using ZenTask.Services.Data;
using ZenTask.ViewModels.Base;
using Task = ZenTask.Models.Task;
using TaskStatus = ZenTask.Models.TaskStatus;

namespace ZenTask.ViewModels
{
    /// <summary>
    /// ViewModel for task creation and editing form
    /// </summary>
    public class TaskFormViewModel : ViewModelBase
    {
        private readonly TaskRepository _taskRepository;
        private readonly CategoryRepository _categoryRepository;
        private readonly TagRepository _tagRepository;

        #region Form Properties

        private string _taskTitle;
        private string _taskDescription;
        private DateTime? _taskDueDate;
        private TaskPriority _taskPriority;
        private int _taskCategoryId;
        private ObservableCollection<SubTask> _taskSubTasks;
        private ObservableCollection<int> _taskTagIds;
        private bool _isEditMode;
        private bool _isPopupOpen;
        private int _taskId;
        private string _titleError;
        private bool _hasTitleError;

        // Form field properties
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

        public bool IsPopupOpen
        {
            get => _isPopupOpen;
            set => SetProperty(ref _isPopupOpen, value);
        }

        public int TaskId
        {
            get => _taskId;
            set => SetProperty(ref _taskId, value);
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

        #endregion

        #region Collections

        private ObservableCollection<Category> _categories;
        private ObservableCollection<Tag> _tags;

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

        #endregion

        #region Commands

        public ICommand ClosePopupCommand { get; }
        public ICommand SaveTaskCommand { get; }
        public ICommand AddSubTaskCommand { get; }
        public ICommand RemoveSubTaskCommand { get; }
        public ICommand ToggleSubTaskCommand { get; }
        public ICommand ToggleTagCommand { get; }

        #endregion

        #region Events

        // Event to notify task list after successful save
        public event EventHandler<Task> TaskSaved;

        #endregion

        public TaskFormViewModel(TaskRepository taskRepository, CategoryRepository categoryRepository, TagRepository tagRepository)
        {
            _taskRepository = taskRepository;
            _categoryRepository = categoryRepository;
            _tagRepository = tagRepository;

            // Initialize collections
            Categories = new ObservableCollection<Category>();
            Tags = new ObservableCollection<Tag>();
            TaskSubTasks = new ObservableCollection<SubTask>();
            TaskTagIds = new ObservableCollection<int>();

            // Initialize commands
            ClosePopupCommand = new RelayCommand(_ => ClosePopup());
            SaveTaskCommand = new RelayCommand(_ => SaveTaskAsync());
            AddSubTaskCommand = new RelayCommand(_ => AddSubTask());
            RemoveSubTaskCommand = new RelayCommand(param => RemoveSubTask((int)param));
            ToggleSubTaskCommand = new RelayCommand(param => ToggleSubTaskAsync((int)param));
            ToggleTagCommand = new RelayCommand(param => ToggleTag((int)param));

            // Default values
            ResetForm();
        }

        #region Public Methods

        /// <summary>
        /// Load categories and tags
        /// </summary>
        /// <summary>
        /// Load categories and tags
        /// </summary>
        public async Task LoadReferenceDataAsync()
        {
            await ExecuteAsync(async () =>
            {
                // Load categories
                var categories = await _categoryRepository.GetAllAsync();
                Categories.Clear();
                foreach (var category in categories)
                {
                    Categories.Add(category);
                }

                // Load tags
                var tags = await _tagRepository.GetAllAsync();
                Tags.Clear();
                foreach (var tag in tags)
                {
                    Tags.Add(tag);
                }

                // Ensure there's at least one category selected
                if (Categories.Count > 0 && TaskCategoryId <= 0)
                {
                    TaskCategoryId = Categories[0].Id;
                }
            }, "loading reference data");
        }

        /// <summary>
        /// Create a new task in the form
        /// </summary>
        public void CreateNewTask()
        {
            ResetForm();
            IsEditMode = false;
            IsPopupOpen = true;
        }

        /// <summary>
        /// Edit an existing task
        /// </summary>
        public void EditTask(Task task)
        {
            if (task == null) return;

            try
            {
                // Populate form with task details
                TaskId = task.Id;
                TaskTitle = task.Title;
                TaskDescription = task.Description;
                TaskDueDate = task.DueDate;
                TaskPriority = task.Priority;
                TaskCategoryId = task.CategoryId;

                // Clone subtasks to avoid modifying originals directly
                TaskSubTasks = new ObservableCollection<SubTask>();
                foreach (var subTask in task.SubTasks)
                {
                    TaskSubTasks.Add(new SubTask
                    {
                        Id = subTask.Id,
                        TaskId = subTask.TaskId,
                        Title = subTask.Title,
                        IsCompleted = subTask.IsCompleted,
                        DisplayOrder = subTask.DisplayOrder
                    });
                }

                // Clone tag IDs
                TaskTagIds = new ObservableCollection<int>(task.TagIds);

                // Set edit mode
                IsEditMode = true;
                IsPopupOpen = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading task data: {ex.Message}";
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Save the current task
        /// </summary>
        private async void SaveTaskAsync()
        {
            // Validate input
            if (!ValidateForm()) return;

            await ExecuteAsync(async () =>
            {
                Task task;

                if (IsEditMode)
                {
                    // Update existing task
                    task = await _taskRepository.GetByIdAsync(TaskId);
                    if (task == null)
                    {
                        throw new Exception("Task not found");
                    }

                    // Update properties
                    task.Title = TaskTitle;
                    task.Description = TaskDescription;
                    task.DueDate = TaskDueDate;
                    task.Priority = TaskPriority;
                    task.CategoryId = TaskCategoryId;
                    task.SubTasks = TaskSubTasks.ToList();
                    task.TagIds = TaskTagIds.ToList();

                    // Save to database
                    bool success = await _taskRepository.UpdateAsync(task);
                    if (!success)
                    {
                        throw new Exception("Failed to update task");
                    }

                    ShowSuccessNotification("Task Updated", $"Task '{TaskTitle}' has been updated successfully");
                }
                else
                {
                    // Create new task
                    task = new Task
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

                    // Save to database
                    int taskId = await _taskRepository.InsertAsync(task);
                    if (taskId <= 0)
                    {
                        throw new Exception("Failed to create task");
                    }

                    task.Id = taskId;
                    ShowSuccessNotification("Task Created", $"New task '{TaskTitle}' has been created successfully");
                }

                // Notify subscribers of the updated/created task
                TaskSaved?.Invoke(this, task);

                // Close the form
                ClosePopup();
            }, IsEditMode ? "updating task" : "creating task");
        }

        /// <summary>
        /// Validate form inputs
        /// </summary>
        private bool ValidateForm()
        {
            // Clear previous errors
            TitleError = string.Empty;
            HasTitleError = false;

            // Validate title
            if (string.IsNullOrWhiteSpace(TaskTitle))
            {
                TitleError = "Title cannot be empty";
                HasTitleError = true;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Close the popup form
        /// </summary>
        private void ClosePopup()
        {
            IsPopupOpen = false;
            ResetForm();
        }

        /// <summary>
        /// Reset form fields to defaults
        /// </summary>
        private void ResetForm()
        {
            TaskId = 0;
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

            // Clear validation errors
            TitleError = string.Empty;
            HasTitleError = false;
        }

        /// <summary>
        /// Add a new subtask
        /// </summary>
        private void AddSubTask()
        {
            if (TaskSubTasks == null)
            {
                TaskSubTasks = new ObservableCollection<SubTask>();
            }

            // Add a new subtask
            var newSubTask = new SubTask
            {
                Title = "New subtask",
                IsCompleted = false,
                DisplayOrder = TaskSubTasks.Count
            };

            TaskSubTasks.Add(newSubTask);
        }

        /// <summary>
        /// Remove a subtask by index
        /// </summary>
        private void RemoveSubTask(int index)
        {
            if (TaskSubTasks != null && index >= 0 && index < TaskSubTasks.Count)
            {
                TaskSubTasks.RemoveAt(index);

                // Update display order
                for (int i = 0; i < TaskSubTasks.Count; i++)
                {
                    TaskSubTasks[i].DisplayOrder = i;
                }
            }
        }

        /// <summary>
        /// Toggle a subtask's completion status
        /// </summary>
        private async void ToggleSubTaskAsync(int subTaskId)
        {
            try
            {
                // Find the subtask in the list
                var subTask = TaskSubTasks?.FirstOrDefault(s => s.Id == subTaskId);
                if (subTask != null)
                {
                    // Toggle completion state
                    subTask.IsCompleted = !subTask.IsCompleted;

                    // Update in database if it's an existing subtask
                    if (subTaskId > 0 && IsEditMode)
                    {
                        var success = await _taskRepository.UpdateSubTaskCompletionAsync(subTaskId, subTask.IsCompleted);
                        if (!success)
                        {
                            // Revert on failure
                            subTask.IsCompleted = !subTask.IsCompleted;
                            ErrorMessage = "Failed to update subtask status";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error updating subtask: {ex.Message}";
            }
        }

        /// <summary>
        /// Toggle a tag selection
        /// </summary>
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
    }
}