using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

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
}