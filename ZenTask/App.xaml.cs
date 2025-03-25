using System;
using System.Windows;
using System.Windows.Threading;
using ZenTask.Services;
using ZenTask.Services.Data;
using ZenTask.Services.Security;
using ZenTask.Services.Settings;

namespace ZenTask
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // Services
        private SQLiteDataService _dataService;
        private ThemeService _themeService;
        private ErrorHandlingService _errorHandlingService;
        private SettingsService _settingsService;
        private AppInitializationService _initializationService;
        private StartupWindow _startupWindow;

        // Static service accessors (simple service locator pattern)
        public static ThemeService ThemeService { get; private set; }
        public static SQLiteDataService DataService { get; private set; }
        public static ErrorHandlingService ErrorService { get; private set; }
        public static SettingsService SettingsService { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Create services
                _dataService = new SQLiteDataService();
                _themeService = new ThemeService();
                _errorHandlingService = ErrorHandlingService.Instance;
                _settingsService = SettingsService.Instance;

                // Set static accessors
                DataService = _dataService;
                ThemeService = _themeService;
                ErrorService = _errorHandlingService;
                SettingsService = _settingsService;

                // Create initialization service
                _initializationService = new AppInitializationService(
                    _dataService,
                    _themeService,
                    _errorHandlingService,
                    _settingsService);

                // Subscribe to initialization events
                _initializationService.ProgressChanged += InitializationService_ProgressChanged;
                _initializationService.InitializationCompleted += InitializationService_Completed;
                _initializationService.InitializationFailed += InitializationService_Failed;

                // Show startup window
                _startupWindow = new StartupWindow();
                _startupWindow.Show();

                // Begin initialization process
                _initializationService.BeginInitialization();
            }
            catch (Exception ex)
            {
                HandleStartupException(ex);
            }
        }

        private void InitializationService_ProgressChanged(object sender, InitializationProgressEventArgs e)
        {
            // Update startup window with progress
            if (_startupWindow != null)
            {
                _startupWindow.UpdateProgress(e.Status, e.PercentComplete);
            }
        }

        private void InitializationService_Completed(object sender, EventArgs e)
        {
            // Create and show main window
            var mainWindow = _initializationService.CreateMainWindow();

            // Show the main window with a slight delay to ensure smooth transition
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                mainWindow.Show();

                // Close startup window
                _startupWindow?.Close();
                _startupWindow = null;
            }));
        }

        private void InitializationService_Failed(object sender, Exception e)
        {
            HandleStartupException(e);
        }

        private void HandleStartupException(Exception ex)
        {
            // Show error message
            MessageBox.Show(
                $"Failed to start application: {ex.Message}\n\nThe application will now close.",
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            // Shutdown the application
            Current.Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Save settings
            _settingsService?.SaveSettings();

            base.OnExit(e);
        }
    }
}