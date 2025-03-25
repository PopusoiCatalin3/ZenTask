using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ZenTask.Services
{
    /// <summary>
    /// Central service for handling and logging application errors
    /// </summary>
    public class ErrorHandlingService
    {
        private static ErrorHandlingService _instance;
        public static ErrorHandlingService Instance => _instance ??= new ErrorHandlingService();

        private readonly string _logFilePath;
        private const int MaxLogSizeBytes = 5 * 1024 * 1024; // 5MB

        // Event for when an error occurs
        public event EventHandler<ErrorEventArgs> ErrorOccurred;

        private ErrorHandlingService()
        {
            // Set up the log file path in the app's folder
            _logFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ZenTask",
                "logs",
                "zentask.log");

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath));

            // Check log file size and rotate if needed
            RotateLogFileIfNeeded();
        }

        /// <summary>
        /// Set up global exception handling for the application
        /// </summary>
        public void SetupGlobalExceptionHandling()
        {
            // Handle exceptions in the UI thread
            Application.Current.DispatcherUnhandledException += (s, e) =>
            {
                HandleException(e.Exception, "UI Thread Exception");
                e.Handled = true; // Prevent app from crashing
            };

            // Handle exceptions in any thread
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    HandleException(ex, "AppDomain Unhandled Exception");
                }
            };

            // Handle exceptions in tasks
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                HandleException(e.Exception, "Task Exception");
                e.SetObserved(); // Mark as observed to prevent crashing
            };
        }

        /// <summary>
        /// Handle an exception
        /// </summary>
        public void HandleException(Exception ex, string context = "Application Exception")
        {
            // Log the error
            LogError(ex, context);

            // Notify subscribers about the error
            ErrorOccurred?.Invoke(this, new ErrorEventArgs(ex, context));

            // Show error dialog for serious errors
            if (IsSerious(ex))
            {
                ShowErrorDialog(ex, context);
            }
        }

        /// <summary>
        /// Log an error message without an exception
        /// </summary>
        public void LogError(string message, string context = "Application Error")
        {
            try
            {
                // Log to debug output
                Debug.WriteLine($"ERROR in {context}: {message}");

                // Log to file
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{context}] {message}{Environment.NewLine}";
                File.AppendAllText(_logFilePath, logMessage);
            }
            catch (Exception ex)
            {
                // If logging fails, at least output to debug
                Debug.WriteLine($"Failed to log error: {ex.Message}");
            }
        }

        /// <summary>
        /// Log an exception
        /// </summary>
        public void LogError(Exception ex, string context = "Application Exception")
        {
            try
            {
                // Log to debug output
                Debug.WriteLine($"EXCEPTION in {context}: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);

                // Build detailed error message
                var sb = new StringBuilder();
                sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{context}]");
                sb.AppendLine($"Message: {ex.Message}");
                sb.AppendLine($"Type: {ex.GetType().FullName}");
                sb.AppendLine($"StackTrace: {ex.StackTrace}");

                // Include inner exception details if available
                if (ex.InnerException != null)
                {
                    sb.AppendLine("Inner Exception:");
                    sb.AppendLine($"Message: {ex.InnerException.Message}");
                    sb.AppendLine($"Type: {ex.InnerException.GetType().FullName}");
                    sb.AppendLine($"StackTrace: {ex.InnerException.StackTrace}");
                }

                sb.AppendLine(new string('-', 80));

                // Write to log file
                File.AppendAllText(_logFilePath, sb.ToString());
            }
            catch (Exception logEx)
            {
                // If logging fails, at least output to debug
                Debug.WriteLine($"Failed to log exception: {logEx.Message}");
            }
        }

        /// <summary>
        /// Show an error dialog for serious errors
        /// </summary>
        private void ShowErrorDialog(Exception ex, string context)
        {
            try
            {
                // Get main window to center the dialog
                var mainWindow = Application.Current?.MainWindow;

                // Create a comprehensive error message
                var message = $"An error occurred in {context}:\n\n{ex.Message}";

                // Show the error dialog
                MessageBox.Show(
                    mainWindow,
                    message,
                    "Application Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch
            {
                // If showing dialog fails, at least we logged the original error
            }
        }

        /// <summary>
        /// Show a toast notification for errors
        /// </summary>
        public void ShowErrorToast(string title, string message)
        {
            try
            {
                // Log the error
                LogError(message, title);

                // Show toast notification if main window is available
                var mainWindow = Application.Current?.MainWindow as MainWindow;
                mainWindow?.ShowErrorNotification(title, message);
            }
            catch
            {
                // If toast fails, at least we logged the error
            }
        }

        /// <summary>
        /// Check if an exception is serious enough to show a dialog
        /// </summary>
        private bool IsSerious(Exception ex)
        {
            // Consider these exceptions serious:
            return ex is OutOfMemoryException
                || ex is StackOverflowException
                || ex is AccessViolationException
                || ex is System.Threading.ThreadAbortException
                || ex is System.IO.IOException
                || ex is System.Data.SQLite.SQLiteException;
        }

        /// <summary>
        /// Rotate log file if it gets too large
        /// </summary>
        private void RotateLogFileIfNeeded()
        {
            try
            {
                if (File.Exists(_logFilePath))
                {
                    var fileInfo = new FileInfo(_logFilePath);
                    if (fileInfo.Length > MaxLogSizeBytes)
                    {
                        // Rename the current log file with a timestamp
                        string backupPath = Path.Combine(
                            Path.GetDirectoryName(_logFilePath),
                            $"zentask_{DateTime.Now:yyyyMMdd_HHmmss}.log");

                        File.Move(_logFilePath, backupPath);

                        // Delete old log files if there are more than 5
                        CleanupOldLogFiles();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to rotate log file: {ex.Message}");
            }
        }

        /// <summary>
        /// Clean up old log files, keeping only the 5 most recent
        /// </summary>
        private void CleanupOldLogFiles()
        {
            try
            {
                var directory = Path.GetDirectoryName(_logFilePath);
                var logFiles = Directory.GetFiles(directory, "zentask_*.log")
                    .OrderByDescending(f => f)
                    .Skip(5); // Keep 5 most recent

                foreach (var file in logFiles)
                {
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to clean up old log files: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Event args for error notification
    /// </summary>
    public class ErrorEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public string Context { get; }

        public ErrorEventArgs(Exception exception, string context)
        {
            Exception = exception;
            Context = context;
        }
    }

    /// <summary>
    /// Extension methods for easy error handling
    /// </summary>
    public static class ErrorHandlingExtensions
    {
        /// <summary>
        /// Try to execute an action with error handling
        /// </summary>
        public static bool TryExecute(this Action action, string context = "Action")
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                ErrorHandlingService.Instance.HandleException(ex, context);
                return false;
            }
        }

        /// <summary>
        /// Try to execute a function with error handling
        /// </summary>
        public static bool TryExecute<T>(this Func<T> func, out T result, string context = "Function")
        {
            try
            {
                result = func();
                return true;
            }
            catch (Exception ex)
            {
                ErrorHandlingService.Instance.HandleException(ex, context);
                result = default;
                return false;
            }
        }

        /// <summary>
        /// Execute an async task with error handling
        /// </summary>
        public static async Task ExecuteWithErrorHandlingAsync(this Task task, string context = "Async Task")
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                ErrorHandlingService.Instance.HandleException(ex, context);
                throw; // Rethrow to let caller know operation failed
            }
        }

        /// <summary>
        /// Execute an async function with error handling
        /// </summary>
        public static async Task<T> ExecuteWithErrorHandlingAsync<T>(this Task<T> task, string context = "Async Function", T defaultValue = default)
        {
            try
            {
                return await task;
            }
            catch (Exception ex)
            {
                ErrorHandlingService.Instance.HandleException(ex, context);
                return defaultValue;
            }
        }
    }
}