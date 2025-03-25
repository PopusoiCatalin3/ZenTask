using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ZenTask.Services.Data;
using ZenTask.Services.Security;
using ZenTask.Services.Settings;

namespace ZenTask.Services
{
    /// <summary>
    /// Handles application initialization in an optimized, staged manner
    /// </summary>
    public class AppInitializationService
    {
        // Dependencies
        private readonly SQLiteDataService _dataService;
        private readonly ThemeService _themeService;
        private readonly ErrorHandlingService _errorHandlingService;
        private readonly SettingsService _settingsService;

        // Initialization state
        private bool _isDatabaseInitialized;
        private bool _isThemeInitialized;
        private bool _isServicesInitialized;

        // Event to track initialization progress
        public event EventHandler<InitializationProgressEventArgs> ProgressChanged;
        public event EventHandler InitializationCompleted;
        public event EventHandler<Exception> InitializationFailed;

        public AppInitializationService(
            SQLiteDataService dataService,
            ThemeService themeService,
            ErrorHandlingService errorHandlingService,
            SettingsService settingsService)
        {
            _dataService = dataService;
            _themeService = themeService;
            _errorHandlingService = errorHandlingService;
            _settingsService = settingsService;
        }

        /// <summary>
        /// Start the initialization process in stages
        /// </summary>
        public void BeginInitialization()
        {
            try
            {
                ReportProgress("Starting application...", 0);

                // Set up error handling first
                _errorHandlingService.SetupGlobalExceptionHandling();

                // Start the staged initialization on a background dispatcher
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(async () =>
                {
                    try
                    {
                        await InitializeInStagesAsync();
                        InitializationCompleted?.Invoke(this, EventArgs.Empty);
                    }
                    catch (Exception ex)
                    {
                        _errorHandlingService.HandleException(ex, "Application Initialization");
                        InitializationFailed?.Invoke(this, ex);
                    }
                }));
            }
            catch (Exception ex)
            {
                _errorHandlingService.HandleException(ex, "Application Startup");
                InitializationFailed?.Invoke(this, ex);
            }
        }

        /// <summary>
        /// Initialize the application in stages to improve perceived performance
        /// </summary>
        private async Task InitializeInStagesAsync()
        {
            // Stage 1: Initialize critical services
            ReportProgress("Initializing core services...", 10);
            await Task.Run(() => InitializeCoreServices());
            await Task.Delay(50); // Small delay to allow UI to update

            // Stage 2: Initialize database
            ReportProgress("Preparing database...", 25);
            await InitializeDatabaseAsync();
            await Task.Delay(50); // Small delay to allow UI to update

            // Stage 3: Load theme settings
            ReportProgress("Loading visual settings...", 50);
            await InitializeThemeAsync();
            await Task.Delay(50); // Small delay to allow UI to update

            // Stage 4: Initialize remaining services
            ReportProgress("Loading application services...", 75);
            await InitializeServicesAsync();
            await Task.Delay(50); // Small delay to allow UI to update

            // Complete
            ReportProgress("Ready", 100);
        }

        /// <summary>
        /// Initialize core services that are needed immediately
        /// </summary>
        private void InitializeCoreServices()
        {
            try
            {
                // Services are already injected in the constructor
                Debug.WriteLine("Core services initialized");
            }
            catch (Exception ex)
            {
                _errorHandlingService.HandleException(ex, "Core Services Initialization");
                throw; // Rethrow to abort initialization
            }
        }

        /// <summary>
        /// Initialize database connections and schema
        /// </summary>
        private async Task InitializeDatabaseAsync()
        {
            if (_isDatabaseInitialized) return;

            try
            {
                // Initialize basic database
                _dataService.Initialize();

                // Create database schema if needed
                await _dataService.InitializeTablesAsync();

                _isDatabaseInitialized = true;
                Debug.WriteLine("Database initialized");
            }
            catch (Exception ex)
            {
                _errorHandlingService.HandleException(ex, "Database Initialization");
                throw; // Rethrow to abort initialization
            }
        }

        /// <summary>
        /// Initialize theme based on settings
        /// </summary>
        private async Task InitializeThemeAsync()
        {
            if (_isThemeInitialized) return;

            try
            {
                await Task.Run(() =>
                {
                    // Get theme setting from settings service
                    string themeSetting = _settingsService.ThemeSetting;
                    Debug.WriteLine($"Theme setting from settings service: {themeSetting}");

                    // Determine the theme
                    var theme = themeSetting?.Equals("Dark", StringComparison.OrdinalIgnoreCase) == true
                        ? ThemeType.Dark
                        : ThemeType.Light;

                    // Apply theme (must happen on UI thread)
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _themeService.Initialize(theme);
                    });
                });

                _isThemeInitialized = true;
                Debug.WriteLine("Theme initialized");
            }
            catch (Exception ex)
            {
                _errorHandlingService.HandleException(ex, "Theme Initialization");
                // Don't rethrow - theme is not critical
            }
        }

        /// <summary>
        /// Initialize remaining application services
        /// </summary>
        private async Task InitializeServicesAsync()
        {
            if (_isServicesInitialized) return;

            try
            {
                // Initialize any other services that aren't critical for startup
                // For example, background tasks, sync services, etc.
                await Task.Delay(100); // Placeholder for actual initialization

                _isServicesInitialized = true;
                Debug.WriteLine("Application services initialized");
            }
            catch (Exception ex)
            {
                _errorHandlingService.HandleException(ex, "Services Initialization");
                // Don't rethrow - these services aren't critical
            }
        }

        /// <summary>
        /// Report initialization progress
        /// </summary>
        private void ReportProgress(string status, int percentComplete)
        {
            Debug.WriteLine($"Initialization: {status} - {percentComplete}%");

            ProgressChanged?.Invoke(this, new InitializationProgressEventArgs(status, percentComplete));
        }

        /// <summary>
        /// Create the application's main window
        /// </summary>
        public MainWindow CreateMainWindow()
        {
            var mainWindow = new MainWindow();
            mainWindow.CompleteInitialization();
            return mainWindow;
        }
    }

    /// <summary>
    /// Event args for reporting initialization progress
    /// </summary>
    public class InitializationProgressEventArgs : EventArgs
    {
        public string Status { get; }
        public int PercentComplete { get; }

        public InitializationProgressEventArgs(string status, int percentComplete)
        {
            Status = status;
            PercentComplete = percentComplete;
        }
    }
}