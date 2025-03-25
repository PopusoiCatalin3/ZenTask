using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Animation;
using System.Diagnostics;
using System.Windows.Threading;
using ZenTask.Utils;

namespace ZenTask.Services.Settings
{
    public enum ThemeType
    {
        Light,
        Dark
    }

    public class ThemeService
    {
        // Define URIs for theme dictionaries - using component-relative URIs
        private readonly Uri _lightThemeUri = new Uri("/Resources/Themes/ThemeColors.xaml", UriKind.Relative);
        private readonly Uri _darkThemeUri = new Uri("/Resources/Themes/DarkThemeColors.xaml", UriKind.Relative);

        // Pre-loaded dictionaries for faster switching
        private ResourceDictionary _lightDict;
        private ResourceDictionary _darkDict;
        private ResourceDictionary _currentThemeDict;

        // Current theme tracker
        public ThemeType CurrentTheme { get; private set; } = ThemeType.Light;

        // Event for theme change notifications
        public event EventHandler ThemeChanged;

        /// <summary>
        /// Initialize the theme service
        /// </summary>
        public void Initialize(ThemeType initialTheme = ThemeType.Light)
        {
            Debug.WriteLine($"ThemeService.Initialize called with {initialTheme}");

            // Preload both theme dictionaries
            PreloadThemeDictionaries();

            // Set initial theme
            CurrentTheme = initialTheme;

            // Apply on UI thread
            Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                Debug.WriteLine($"Applying initial theme: {initialTheme}");
                ApplyTheme(initialTheme);
            }));
        }

        /// <summary>
        /// Preload theme dictionaries to avoid delay when switching
        /// </summary>
        private void PreloadThemeDictionaries()
        {
            try
            {
                // Create light theme dictionary if not already loaded
                if (_lightDict == null)
                {
                    _lightDict = new ResourceDictionary { Source = _lightThemeUri };
                }

                // Create dark theme dictionary if not already loaded
                if (_darkDict == null)
                {
                    _darkDict = new ResourceDictionary { Source = _darkThemeUri };
                }

                Debug.WriteLine("Theme dictionaries preloaded successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error preloading theme dictionaries: {ex.Message}");
            }
        }

        /// <summary>
        /// Set active theme
        /// </summary>
        public void SetTheme(ThemeType theme)
        {
            if (theme == CurrentTheme)
                return;

            Debug.WriteLine($"Setting theme to {theme}");
            CurrentTheme = theme;

            // Apply theme on UI thread without blocking
            Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                ApplyTheme(theme);

                // Save theme setting for persistence
                Properties.Settings.Default.ThemeSetting = theme == ThemeType.Dark ? "Dark" : "Light";
                Properties.Settings.Default.Save();
            }));
        }

        /// <summary>
        /// Apply the selected theme
        /// </summary>
        private void ApplyTheme(ThemeType theme)
        {
            try
            {
                // Get the theme dictionary
                ResourceDictionary themeDict = GetThemeDictionary(theme);

                // If we're already using this dictionary, exit early
                if (_currentThemeDict == themeDict)
                    return;

                // Find and remove the current theme dictionary if it exists
                if (_currentThemeDict != null)
                {
                    Application.Current.Resources.MergedDictionaries.Remove(_currentThemeDict);
                }

                // Add the new theme dictionary
                Application.Current.Resources.MergedDictionaries.Add(themeDict);
                _currentThemeDict = themeDict;

                // Notify about theme change
                ThemeChanged?.Invoke(this, EventArgs.Empty);

                Debug.WriteLine($"Theme changed to {theme}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying theme: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the appropriate theme dictionary
        /// </summary>
        private ResourceDictionary GetThemeDictionary(ThemeType theme)
        {
            return theme == ThemeType.Dark ? _darkDict : _lightDict;
        }

        /// <summary>
        /// Toggle between light and dark themes
        /// </summary>
        public void ToggleTheme()
        {
            SetTheme(CurrentTheme == ThemeType.Light ? ThemeType.Dark : ThemeType.Light);
        }
    }

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

    public static class SettingsHelper
    {
        /// <summary>
        /// Safely reads a setting value with error handling
        /// </summary>
        public static T GetSetting<T>(string settingName, T defaultValue)
        {
            try
            {
                var properties = Properties.Settings.Default;
                object value = properties[settingName];

                if (value != null && value is T typedValue)
                {
                    return typedValue;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading setting {settingName}: {ex.Message}");
            }

            return defaultValue;
        }

        /// <summary>
        /// Asynchronously saves a setting value
        /// </summary>
        public static async Task SaveSettingAsync<T>(string settingName, T value)
        {
            await Task.Run(() =>
            {
                try
                {
                    var properties = Properties.Settings.Default;
                    properties[settingName] = value;
                    properties.Save();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving setting {settingName}: {ex.Message}");
                }
            });
        }
    }
}