using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace ZenTask.ViewModels.Base
{
    /// <summary>
    /// Enhanced base view model with common functionality for all view models
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

        #region Loading and Error State

        private bool _isLoading;
        private string _errorMessage;
        private bool _hasError;

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

        #endregion

        #region Async Helpers

        /// <summary>
        /// Helper method to safely execute async operations with proper state management
        /// </summary>
        protected async Task ExecuteAsync(Func<Task> operation, string errorContext = "operation")
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                await operation();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error during {errorContext}: {ex.Message}";

                // Show error notification if available
                ShowErrorNotification($"Error during {errorContext}", ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Helper method to safely execute async operations with a return value
        /// </summary>
        protected async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string errorContext = "operation", T defaultValue = default)
        {
            if (IsLoading) return defaultValue;

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                return await operation();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error during {errorContext}: {ex.Message}";

                // Show error notification if available
                ShowErrorNotification($"Error during {errorContext}", ex.Message);

                return defaultValue;
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Notification Helpers

        /// <summary>
        /// Show an error notification
        /// </summary>
        protected void ShowErrorNotification(string title, string message)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.ShowErrorNotification(title, message);
        }

        /// <summary>
        /// Show a success notification
        /// </summary>
        protected void ShowSuccessNotification(string title, string message)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.ShowSuccessNotification(title, message);
        }

        /// <summary>
        /// Show a general notification
        /// </summary>
        protected void ShowNotification(string title, string message, ZenTask.Controls.ToastType type = ZenTask.Controls.ToastType.Info)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.ShowNotification(title, message, type);
        }

        #endregion
    }
}