using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ZenTask.Services.Data;
using ZenTask.Services.Settings;

namespace ZenTask.Utils
{
    /// <summary>
    /// Handles progressive async loading of application resources
    /// to prevent UI freezing
    /// </summary>
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