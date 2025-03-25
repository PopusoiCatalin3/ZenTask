using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using ZenTask.Services.Data;
using ZenTask.Services.Settings;

namespace ZenTask
{
    /// <summary>
    /// Interaction logic for StartupWindow.xaml
    /// </summary>
    public partial class StartupWindow : Window
    {
        private readonly BackgroundWorker _worker = new BackgroundWorker();
        private MainWindow _mainWindow;

        public StartupWindow()
        {
            // Set up this window before loading any resources
            InitializeComponent();

            // Configure background worker
            _worker.WorkerReportsProgress = true;
            _worker.WorkerSupportsCancellation = true;
            _worker.DoWork += Worker_DoWork;
            _worker.ProgressChanged += Worker_ProgressChanged;
            _worker.RunWorkerCompleted += Worker_RunWorkerCompleted;

            // Start the worker when window is loaded
            this.Loaded += (s, e) => {
                _worker.RunWorkerAsync();
            };
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // Step 1: Initialize database and services first before creating UI
                _worker.ReportProgress(20, "Initializing database...");
                App.DataService.Initialize();
                Thread.Sleep(100);

                // Step 2: Create database tables
                _worker.ReportProgress(40, "Setting up database...");
                App.DataService.InitializeTablesAsync().GetAwaiter().GetResult();
                Thread.Sleep(100);

                // Step 3: Load theme settings
                _worker.ReportProgress(60, "Loading visual settings...");
                string themeSetting = Properties.Settings.Default.ThemeSetting;
                var theme = themeSetting?.Equals("Dark", StringComparison.OrdinalIgnoreCase) == true
                    ? Services.Settings.ThemeType.Dark
                    : Services.Settings.ThemeType.Light;

                _worker.ReportProgress(80, "Preparing user interface...");

                // Important: Don't create the MainWindow here as it needs to be on the UI thread
                // We'll do that in the RunWorkerCompleted handler
            }
            catch (Exception ex)
            {
                e.Result = ex;
            }
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Update UI with progress
            LoadingProgress.Value = e.ProgressPercentage;
            if (e.UserState is string message)
            {
                StatusText.Text = message;
            }
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                // Show error and exit
                MessageBox.Show($"Error initializing application: {e.Error.Message}",
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }

            if (e.Result is Exception ex)
            {
                // Show exception from worker and exit
                MessageBox.Show($"Error initializing application: {ex.Message}",
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }

            try
            {
                // Now we're on the UI thread, create and initialize the main window
                StatusText.Text = "Launching application...";
                LoadingProgress.Value = 90;

                // Initialize theme
                string themeSetting = Properties.Settings.Default.ThemeSetting;
                Debug.WriteLine($"Theme setting from properties: {themeSetting}");

                var theme = themeSetting?.Equals("Dark", StringComparison.OrdinalIgnoreCase) == true
                    ? Services.Settings.ThemeType.Dark
                    : Services.Settings.ThemeType.Light;

                Debug.WriteLine($"Initializing theme service with: {theme}");

                // Initialize theme
                App.ThemeService.Initialize(theme);
                Debug.WriteLine("Theme service initialized");
                // Create main window on UI thread
                _mainWindow = new MainWindow();

                // Complete initialization before showing
                _mainWindow.CompleteInitialization();

                // Show the main window
                _mainWindow.Show();

                // Close this window with a slight delay
                DispatcherTimer timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(300)
                };
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    this.Close();
                };
                timer.Start();
            }
            catch (Exception finalEx)
            {
                MessageBox.Show($"Error during final initialization: {finalEx.Message}",
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }
    }
    public class AsyncApplicationLoader
    {
        private readonly SQLiteDataService _dataService;
        private readonly ThemeService _themeService;

        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;
        public event EventHandler LoadingCompleted;

        private int _totalSteps = 3; // Adjust based on initialization steps
        private int _currentStep = 0;

        public AsyncApplicationLoader(SQLiteDataService dataService, ThemeService themeService)
        {
            _dataService = dataService;
            _themeService = themeService;
        }

        /// <summary>
        /// Starts loading the application in stages
        /// </summary>
        public void BeginLoading()
        {
            ReportProgress("Starting application...", 0);

            // Immediately start the loading sequence with lowest priority
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(async () =>
            {
                try
                {
                    // Stage 1: Initialize basic database
                    ReportProgress("Preparing database...", 10);
                    await Task.Run(() => _dataService.Initialize());
                    _currentStep++;

                    // Stage 2: Create database tables if needed
                    ReportProgress("Setting up database...", 40);
                    await Task.Run(() => _dataService.InitializeTablesAsync());
                    _currentStep++;

                    // Stage 3: Load theme settings and apply
                    ReportProgress("Loading visual settings...", 70);
                    ThemeType theme = ThemeType.Light;
                    await Task.Run(() =>
                    {
                        try
                        {
                            string themeSetting = Properties.Settings.Default.ThemeSetting;
                            theme = themeSetting?.Equals("Dark", StringComparison.OrdinalIgnoreCase) == true ?
                                ThemeType.Dark : ThemeType.Light;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error loading theme: {ex.Message}");
                        }
                    });

                    // Apply theme (must happen on UI thread)
                    _themeService.Initialize(theme);
                    _currentStep++;

                    // Complete!
                    ReportProgress("Loading complete!", 100);
                    LoadingCompleted?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error during async loading: {ex.Message}");
                    MessageBox.Show($"Error during application initialization: {ex.Message}",
                        "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }));
        }

        private void ReportProgress(string message, int percentageOverride = -1)
        {
            int percentage = percentageOverride >= 0 ? percentageOverride :
                (int)Math.Floor((_currentStep / (double)_totalSteps) * 100);

            Debug.WriteLine($"Loading Progress: {percentage}% - {message}");
            ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(percentage, message));
        }
    }
}