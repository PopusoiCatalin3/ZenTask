using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ZenTask.Services.Settings;

namespace ZenTask.Utils
{
    /// <summary>
    /// Monitors theme settings and applies changes
    /// </summary>
    public class ThemeDetector
    {
        private readonly ThemeService _themeService;
        private readonly DispatcherTimer _timer;
        private string _lastThemeSetting;
        private const string ThemeSettingName = "ThemeSetting";

        public ThemeDetector(ThemeService themeService)
        {
            _themeService = themeService;
            _lastThemeSetting = SettingsHelper.GetSetting(ThemeSettingName, "Light");

            // Apply initial theme
            ApplyThemeSetting(_lastThemeSetting);

            // Set up timer to check for changes
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Check for changes on a background thread
            Task.Run(() => CheckForThemeChanges());
        }

        private async Task CheckForThemeChanges()
        {
            try
            {
                // Read current theme setting
                string currentSetting = SettingsHelper.GetSetting(ThemeSettingName, "Light");

                // If theme setting has changed, apply new theme
                if (currentSetting != _lastThemeSetting)
                {
                    _lastThemeSetting = currentSetting;

                    // Switch to UI thread to apply theme
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        ApplyThemeSetting(currentSetting);
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking theme changes: {ex.Message}");
            }
        }

        private void ApplyThemeSetting(string themeSetting)
        {
            try
            {
                // Convert string setting to theme type
                ThemeType theme = themeSetting.Equals("Dark", StringComparison.OrdinalIgnoreCase)
                    ? ThemeType.Dark
                    : ThemeType.Light;

                // Only change if different from current
                if (_themeService.CurrentTheme != theme)
                {
                    _themeService.SetTheme(theme);
                    System.Diagnostics.Debug.WriteLine($"Theme changed to {theme}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying theme: {ex.Message}");
            }
        }

        public void Stop()
        {
            _timer.Stop();
        }
    }
}