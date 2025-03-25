using System;
using System.Threading.Tasks;

namespace ZenTask.Utils
{
    /// <summary>
    /// Helper class to safely access and save application settings
    /// without blocking the UI thread
    /// </summary>
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