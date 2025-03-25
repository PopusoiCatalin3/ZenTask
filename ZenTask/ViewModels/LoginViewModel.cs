using System;
using System.Threading.Tasks;
using System.Windows;
using ZenTask.Services.Security;
using ZenTask.ViewModels.Base;

namespace ZenTask.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly AuthService _authService;
        private readonly Action _onLoginSuccess;

        private string _username;
        private string _email;
        private string _firstName;
        private string _lastName;
        private string _errorMessage;
        private bool _hasError;
        private bool _isProcessing;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string FirstName
        {
            get => _firstName;
            set => SetProperty(ref _firstName, value);
        }

        public string LastName
        {
            get => _lastName;
            set => SetProperty(ref _lastName, value);
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

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public LoginViewModel(AuthService authService, Action onLoginSuccess)
        {
            _authService = authService;
            _onLoginSuccess = onLoginSuccess;
        }

        public async void LoginWithPassword(string password)
        {
            if (IsProcessing)
                return;

            try
            {
                IsProcessing = true;
                ErrorMessage = string.Empty;

                // Run authentication on background thread
                bool success = await Task.Run(() => _authService.LoginAsync(Username, password).Result);

                if (success)
                {
                    _onLoginSuccess?.Invoke();
                }
                else
                {
                    ErrorMessage = "Invalid username or password";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Login failed: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        public async void RegisterWithPassword(string password)
        {
            if (IsProcessing)
                return;

            try
            {
                IsProcessing = true;
                ErrorMessage = string.Empty;

                // Run registration on background thread
                bool success = await Task.Run(() => _authService.RegisterAsync(Username, Email, password, FirstName, LastName).Result);

                if (success)
                {
                    MessageBox.Show("Registration successful! You can now login.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearRegistrationFields();
                }
                else
                {
                    ErrorMessage = "Registration failed. Username or email may already be in use.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Registration failed: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void ClearRegistrationFields()
        {
            Username = string.Empty;
            Email = string.Empty;
            FirstName = string.Empty;
            LastName = string.Empty;
            ErrorMessage = string.Empty;
        }
    }
}