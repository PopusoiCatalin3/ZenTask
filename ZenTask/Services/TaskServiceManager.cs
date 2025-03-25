using System;
using System.Diagnostics;
using ZenTask.Models;
using TaskStatus = ZenTask.Models.TaskStatus;

namespace ZenTask.Services
{
    /// <summary>
    /// Singleton service that coordinates task operations across multiple views
    /// </summary>
    public class TaskServiceManager
    {
        // Singleton instance
        private static TaskServiceManager _instance;
        public static TaskServiceManager Instance => _instance ?? (_instance = new TaskServiceManager());

        // Events for task operations
        public event EventHandler<TaskStatusChangedEventArgs> TaskStatusChanged;
        public event EventHandler<int> TaskDeleted;
        public event EventHandler<Models.Task> TaskCreated;
        public event EventHandler<Models.Task> TaskUpdated;
        public event EventHandler TasksReloaded;

        // Private constructor (singleton pattern)
        private TaskServiceManager()
        {
        }

        /// <summary>
        /// Notifies subscribers that a task's status has changed
        /// </summary>
        public void NotifyTaskStatusChanged(int taskId, TaskStatus newStatus)
        {
            Debug.WriteLine($"TaskServiceManager: Task {taskId} status changed to {newStatus}");
            TaskStatusChanged?.Invoke(this, new TaskStatusChangedEventArgs(taskId, newStatus));
        }

        /// <summary>
        /// Notifies subscribers that a task has been deleted
        /// </summary>
        public void NotifyTaskDeleted(int taskId)
        {
            Debug.WriteLine($"TaskServiceManager: Task {taskId} deleted");
            TaskDeleted?.Invoke(this, taskId);
        }

        /// <summary>
        /// Notifies subscribers that a task has been created
        /// </summary>
        public void NotifyTaskCreated(Models.Task task)
        {
            Debug.WriteLine($"TaskServiceManager: Task {task.Id} created");
            TaskCreated?.Invoke(this, task);
        }

        /// <summary>
        /// Notifies subscribers that a task has been updated
        /// </summary>
        public void NotifyTaskUpdated(Models.Task task)
        {
            Debug.WriteLine($"TaskServiceManager: Task {task.Id} updated");
            TaskUpdated?.Invoke(this, task);
        }

        /// <summary>
        /// Notifies subscribers that tasks have been reloaded and should refresh
        /// </summary>
        public void NotifyTasksReloaded()
        {
            Debug.WriteLine("TaskServiceManager: Tasks reloaded notification");
            TasksReloaded?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Event arguments for task status changes
    /// </summary>
    public class TaskStatusChangedEventArgs : EventArgs
    {
        public int TaskId { get; }
        public TaskStatus NewStatus { get; }

        public TaskStatusChangedEventArgs(int taskId, TaskStatus newStatus)
        {
            TaskId = taskId;
            NewStatus = newStatus;
        }
    }
}