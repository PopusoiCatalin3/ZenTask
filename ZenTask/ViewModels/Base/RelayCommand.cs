using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ZenTask.ViewModels.Base
{
    /// <summary>
    /// Basic implementation of ICommand for view model binding
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>
    /// Async implementation of ICommand for view model binding
    /// </summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object, Task> _execute;
        private readonly Func<object, bool> _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<object, Task> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return !_isExecuting && (_canExecute == null || _canExecute(parameter));
        }

        public async void Execute(object parameter)
        {
            if (!CanExecute(parameter))
                return;

            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();

                await _execute(parameter);
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>
    /// Command factory for creating common command types
    /// </summary>
    public static class CommandFactory
    {
        /// <summary>
        /// Create a simple relay command
        /// </summary>
        public static RelayCommand Create(Action<object> execute, Func<object, bool> canExecute = null)
        {
            return new RelayCommand(execute, canExecute);
        }

        /// <summary>
        /// Create a simple relay command that takes no parameters
        /// </summary>
        public static RelayCommand Create(Action execute, Func<bool> canExecute = null)
        {
            return new RelayCommand(
                _ => execute(),
                canExecute == null ? null : new Func<object, bool>(_ => canExecute()));
        }

        /// <summary>
        /// Create an async relay command
        /// </summary>
        public static AsyncRelayCommand CreateAsync(Func<object, Task> execute, Func<object, bool> canExecute = null)
        {
            return new AsyncRelayCommand(execute, canExecute);
        }

        /// <summary>
        /// Create an async relay command that takes no parameters
        /// </summary>
        public static AsyncRelayCommand CreateAsync(Func<Task> execute, Func<bool> canExecute = null)
        {
            return new AsyncRelayCommand(
                async _ => await execute(),
                canExecute == null ? null : new Func<object, bool>(_ => canExecute()));
        }
    }
}