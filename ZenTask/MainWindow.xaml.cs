using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ZenTask.Services.Data;
using ZenTask.Services.Security;
using ZenTask.Services.Settings;
using ZenTask.ViewModels;

namespace ZenTask
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SQLiteDataService _dataService;
        private UserRepository _userRepository;
        private AuthService _authService;
        private TaskRepository _taskRepository;
        private CategoryRepository _categoryRepository;
        private TagRepository _tagRepository;

        private LoginViewModel _loginViewModel;
        private TaskViewModel _taskViewModel;
        private KanbanViewModel _kanbanViewModel;
        private Views.KanbanView _kanbanView;
        private Views.TaskView _taskView;
        private UserControl _currentView;

        // Storyboards for transitions
        private Storyboard _loginToMainStoryboard;
        private Storyboard _mainToLoginStoryboard;

        // Flag to track initialization
        private bool _isInitialized = false;

        public MainWindow()
        {
            InitializeComponent();

            // Hook up basic event handlers
            this.Loaded += MainWindow_Loaded;
            this.Closed += MainWindow_Closed;

            // Hide content initially
            if (LoginContent != null) LoginContent.Visibility = Visibility.Collapsed;
            if (MainContent != null) MainContent.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Initializes all content and viewmodels
        /// </summary>
        public void CompleteInitialization()
        {
            if (_isInitialized) return;

            try
            {
                _dataService = App.DataService;

                _userRepository = new UserRepository(_dataService, null); 
                _authService = new AuthService(_userRepository);

                _taskRepository = new TaskRepository(_dataService, _authService);
                _categoryRepository = new CategoryRepository(_dataService, _authService);
                _tagRepository = new TagRepository(_dataService, _authService);

                _loginViewModel = new LoginViewModel(_authService, OnLoginSuccess);
                _taskViewModel = new TaskViewModel(_taskRepository, _categoryRepository, _tagRepository);
                _kanbanViewModel = new KanbanViewModel(_taskRepository, _categoryRepository, _tagRepository);

                _loginToMainStoryboard = (Storyboard)FindResource("LoginToMainAnimation");
                _mainToLoginStoryboard = (Storyboard)FindResource("MainToLoginAnimation");

                if (LoginContent != null)
                {
                    LoginContent.DataContext = _loginViewModel;
                    LoginContent.Visibility = Visibility.Visible;
                }

                _taskView = MainTaskView;
                _kanbanView = new Views.KanbanView();

                _taskView.DataContext = _taskViewModel;
                _kanbanView.DataContext = _kanbanViewModel;

                // Set the default view
                _currentView = _taskView;

                // Hook up navigation buttons
                var taskButton = FindName("TasksNavButton") as Button;
                var kanbanButton = FindName("KanbanNavButton") as Button;

                if (taskButton != null)
                {
                    taskButton.Click += (s, e) => SwitchView(_taskView);
                }

                if (kanbanButton != null)
                {
                    kanbanButton.Click += (s, e) => SwitchView(_kanbanView);
                }

                // Setup theme handling
                SetupThemeSwitching();
                UpdateThemeButtonState();

                // Mark as initialized
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing application: {ex.Message}",
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SwitchView(UserControl newView)
        {
            if (newView == _currentView) return;

            // Get the content area
            var contentArea = FindName("ContentArea") as Grid;
            if (contentArea == null) return;

            // Create animation
            Storyboard fadeOutStoryboard = new Storyboard();
            DoubleAnimation fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.2)
            };

            fadeOut.Completed += (s, e) =>
            {
                // Remove current view
                contentArea.Children.Remove(_currentView);

                // Add new view
                contentArea.Children.Add(newView);
                Grid.SetColumn(newView, 0);

                // Create fade in animation
                Storyboard fadeInStoryboard = new Storyboard();
                DoubleAnimation fadeIn = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.3)
                };

                Storyboard.SetTarget(fadeIn, newView);
                Storyboard.SetTargetProperty(fadeIn, new PropertyPath(UIElement.OpacityProperty));
                fadeInStoryboard.Children.Add(fadeIn);

                // Start loading data if it's the Kanban view
                if (newView == _kanbanView && _kanbanViewModel != null)
                {
                    _kanbanViewModel.LoadTasksCommand.Execute(null);
                }

                // Set current view and begin animation
                _currentView = newView;
                fadeInStoryboard.Begin();

                // Update navigation buttons
                UpdateNavigationButtons();
            };

            // Set targets and begin animation
            Storyboard.SetTarget(fadeOut, _currentView);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(UIElement.OpacityProperty));
            fadeOutStoryboard.Children.Add(fadeOut);
            fadeOutStoryboard.Begin();
        }
        private void UpdateNavigationButtons()
        {
            var taskButton = FindName("TasksNavButton") as Button;
            var kanbanButton = FindName("KanbanNavButton") as Button;
            var calendarButton = FindName("CalendarNavButton") as Button;
            var statsButton = FindName("StatsNavButton") as Button;

            if (taskButton != null) taskButton.IsEnabled = _currentView != _taskView;
            if (kanbanButton != null) kanbanButton.IsEnabled = _currentView != _kanbanView;
            if (calendarButton != null) calendarButton.IsEnabled = true;
            if (statsButton != null) statsButton.IsEnabled = true;
        }


        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.ThemeService != null)
            {
                string themeSetting = Properties.Settings.Default.ThemeSetting;
                Debug.WriteLine($"MainWindow: theme setting is {themeSetting}");
                if (themeSetting?.Equals("Dark", StringComparison.OrdinalIgnoreCase) == true)
                {
                    Debug.WriteLine("Explicitly applying Dark theme");
                    App.ThemeService.SetTheme(ThemeType.Dark);
                }
                UpdateThemeButtonState(); // Update the button appearance
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            // Clean up event subscriptions
            if (App.ThemeService != null)
            {
                App.ThemeService.ThemeChanged -= ThemeService_ThemeChanged;
            }
        }

        private void SetupThemeSwitching()
        {
            // Subscribe to theme change events
            if (App.ThemeService != null)
            {
                App.ThemeService.ThemeChanged += ThemeService_ThemeChanged;
            }
        }

        private void ThemeService_ThemeChanged(object sender, EventArgs e)
        {
            // Update the theme button when theme changes
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                UpdateThemeButtonState();
            }));
        }

        private void UpdateThemeButtonState()
        {
            try
            {
                // If ThemeButton exists and ThemeService is initialized, update button content
                if (ThemeButton != null && App.ThemeService != null)
                {
                    ThemeButton.Content = App.ThemeService.CurrentTheme == ThemeType.Light ? "🌙" : "☀️";
                }
            }
            catch (Exception ex)
            {
                // Safely handle any errors without crashing
                System.Diagnostics.Debug.WriteLine($"Error updating theme button: {ex.Message}");
            }
        }

        private void OnLoginSuccess()
        {
            // Trigger the transition animation from login to main content
            _loginToMainStoryboard?.Begin(this);

            // Load data based on current view - on background priority
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
                if (_currentView == _taskView && _taskViewModel != null)
                {
                    _taskViewModel.LoadTasksCommand.Execute(null);
                }
                else if (_currentView == _kanbanView && _kanbanViewModel != null)
                {
                    _kanbanViewModel.LoadTasksCommand.Execute(null);
                }
            }));
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            _authService?.Logout();

            // Reset login fields as needed
            if (_loginViewModel != null)
            {
                _loginViewModel.Username = string.Empty;
                _loginViewModel.Email = string.Empty;
                _loginViewModel.FirstName = string.Empty;
                _loginViewModel.LastName = string.Empty;
                _loginViewModel.ErrorMessage = string.Empty;
            }

            // Find and clear password boxes
            var loginView = FindVisualChild<Views.LoginView>(LoginContent);
            if (loginView != null)
            {
                var loginPasswordBox = FindName("LoginPasswordBox") as PasswordBox ??
                                       FindVisualChild<PasswordBox>(loginView, "LoginPasswordBox");
                var registerPasswordBox = FindVisualChild<PasswordBox>(loginView, "RegisterPasswordBox");
                var confirmPasswordBox = FindVisualChild<PasswordBox>(loginView, "ConfirmPasswordBox");

                if (loginPasswordBox != null) loginPasswordBox.Clear();
                if (registerPasswordBox != null) registerPasswordBox.Clear();
                if (confirmPasswordBox != null) confirmPasswordBox.Clear();
            }

            // Trigger the transition animation from main content to login
            _mainToLoginStoryboard?.Begin(this);
        }

        // Update the ThemeButton_Click method in MainWindow.xaml.cs

        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Play theme transition animation
                PlayThemeTransitionAnimation();

                // Toggle theme directly - service handles saving settings
                if (App.ThemeService != null)
                {
                    App.ThemeService.ToggleTheme();

                    // Show notification
                    string themeMessage = App.ThemeService.CurrentTheme == ThemeType.Light
                        ? "Light theme activated"
                        : "Dark theme activated";

                    ShowNotification("Theme Changed", themeMessage, Controls.ToastType.Info, 2000);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error changing theme: {ex.Message}");
                ShowErrorNotification("Error", $"Could not change theme: {ex.Message}");
            }
        }

        // Add a helper method to get the opposite theme description
        private string GetOppositeThemeDescription()
        {
            if (App.ThemeService == null) return "dark/light";

            return App.ThemeService.CurrentTheme == ThemeType.Light ? "dark" : "light";
        }

        // Helper method to find a child control of a specific type
        private static T FindVisualChild<T>(DependencyObject parent, string childName = null) where T : DependencyObject
        {
            // If parent is null, return null
            if (parent == null) return null;

            // Check if this is the child we're looking for
            if (parent is T && (string.IsNullOrEmpty(childName) ||
                (parent is FrameworkElement element && element.Name == childName)))
            {
                return parent as T;
            }

            // Search for children
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                // Search recursively
                var result = FindVisualChild<T>(child, childName);
                if (result != null)
                    return result;
            }

            return null;
        }
        private void PlayThemeTransitionAnimation()
        {
            try
            {
                // Find the animation resource
                Storyboard themeTransition = this.FindResource("ThemeTransitionAnimation") as Storyboard;
                if (themeTransition != null)
                {
                    // Clone it to avoid issues with reuse
                    Storyboard animationClone = themeTransition.Clone();

                    // Set the target element (ensures animation targets the right element)
                    foreach (var timeline in animationClone.Children)
                    {
                        if (timeline is DoubleAnimation doubleAnim)
                        {
                            Storyboard.SetTargetName(doubleAnim, "ThemeTransitionOverlay");
                        }
                    }

                    // Begin the animation
                    animationClone.Begin(this);
                }
                else
                {
                    Debug.WriteLine("ThemeTransitionAnimation resource not found");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error playing theme transition: {ex.Message}");
            }
        }
        public void ShowNotification(string title, string message, Controls.ToastType toastType = Controls.ToastType.Info, int duration = 3000)
        {
            var toast = new Controls.ToastNotification
            {
                Title = title,
                Message = message,
                ToastType = toastType,
                Duration = duration
            };

            // Subscribe to closed event to remove from panel
            toast.ToastClosed += (s, e) =>
            {
                NotificationsPanel.Dispatcher.BeginInvoke(new Action(() =>
                {
                    NotificationsPanel.Items.Remove(toast);
                }));
            };

            // Add to panel
            NotificationsPanel.Items.Add(toast);

            // Show the toast
            toast.Show();
        }

        /// <summary>
        /// Shows a success notification
        /// </summary>
        public void ShowSuccessNotification(string title, string message, int duration = 3000)
        {
            ShowNotification(title, message, Controls.ToastType.Success, duration);
        }

        /// <summary>
        /// Shows an error notification
        /// </summary>
        public void ShowErrorNotification(string title, string message, int duration = 5000)
        {
            ShowNotification(title, message, Controls.ToastType.Error, duration);
        }

        /// <summary>

    }
}