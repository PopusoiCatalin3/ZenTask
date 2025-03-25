using System;
using System.Windows;
using System.Windows.Threading;
using ZenTask.Services.Data;
using ZenTask.Services.Settings;

namespace ZenTask
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private SQLiteDataService _dataService;
        private ThemeService _themeService;

        // Static service accessor (simple service locator pattern)
        public static ThemeService ThemeService { get; private set; }
        public static SQLiteDataService DataService { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Set up global exception handling
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                _dataService = new SQLiteDataService();
                _themeService = new ThemeService();

                DataService = _dataService;
                ThemeService = _themeService;

                StartupWindow startupWindow = new StartupWindow();
                startupWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting application: {ex.Message}",
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            MessageBox.Show($"An unhandled exception occurred: {ex?.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"An unhandled exception occurred: {e.Exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}