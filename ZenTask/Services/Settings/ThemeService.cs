using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Animation;
using System.Diagnostics;

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
}