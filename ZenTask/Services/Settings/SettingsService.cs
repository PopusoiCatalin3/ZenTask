using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ZenTask.Services.Settings
{
    /// <summary>
    /// Optimized settings service that caches settings and provides cleaner access
    /// </summary>
    public class SettingsService
    {
        private static SettingsService _instance;
        public static SettingsService Instance => _instance ??= new SettingsService();

        private readonly Dictionary<string, object> _settingsCache = new Dictionary<string, object>();
        private readonly string _settingsFilePath;
        private readonly string _defaultSettingsFilePath;
        private bool _isDirty = false;

        // Constants
        private const string ThemeSettingKey = "ThemeSetting";

        // Default values
        private static readonly Dictionary<string, object> DefaultSettings = new Dictionary<string, object>
        {
            { ThemeSettingKey, "Light" },
            { "LastOpenedTasks", new List<int>() },
            { "TaskVelocity", 1.0 },
            { "ShowNotifications", true },
            { "AutoSave", true },
            { "AutoSaveInterval", 5 },
            { "AutoBackup", true },
            { "DatabasePath", "" }
        };

        #region Properties

        /// <summary>
        /// The current theme setting
        /// </summary>
        public string ThemeSetting
        {
            get => GetSetting<string>(ThemeSettingKey);
            set => SetSetting(ThemeSettingKey, value);
        }

        /// <summary>
        /// Whether the app shows notifications
        /// </summary>
        public bool ShowNotifications
        {
            get => GetSetting<bool>("ShowNotifications");
            set => SetSetting("ShowNotifications", value);
        }

        /// <summary>
        /// Whether the app automatically saves changes
        /// </summary>
        public bool AutoSave
        {
            get => GetSetting<bool>("AutoSave");
            set => SetSetting("AutoSave", value);
        }

        /// <summary>
        /// Auto-save interval in minutes
        /// </summary>
        public int AutoSaveInterval
        {
            get => GetSetting<int>("AutoSaveInterval");
            set => SetSetting("AutoSaveInterval", value);
        }

        /// <summary>
        /// Whether the app automatically backs up the database
        /// </summary>
        public bool AutoBackup
        {
            get => GetSetting<bool>("AutoBackup");
            set => SetSetting("AutoBackup", value);
        }

        /// <summary>
        /// Custom path for the SQLite database
        /// </summary>
        public string DatabasePath
        {
            get => GetSetting<string>("DatabasePath");
            set => SetSetting("DatabasePath", value);
        }

        #endregion

        public SettingsService()
        {
            // Set up paths
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appSettingsFolder = Path.Combine(appDataPath, "ZenTask", "Settings");

            // Ensure the settings directory exists
            Directory.CreateDirectory(appSettingsFolder);

            _settingsFilePath = Path.Combine(appSettingsFolder, "settings.xml");
            _defaultSettingsFilePath = Path.Combine(appSettingsFolder, "default_settings.xml");

            // Load settings
            LoadSettings();

            // Set up auto-save
            AppDomain.CurrentDomain.ProcessExit += (s, e) => SaveSettings();
        }

        /// <summary>
        /// Get a setting value, or the default if it doesn't exist
        /// </summary>
        public T GetSetting<T>(string key)
        {
            // First check if it's in the cache
            if (_settingsCache.TryGetValue(key, out var value))
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }

                // Try to convert
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    Debug.WriteLine($"Failed to convert setting {key} from {value?.GetType()} to {typeof(T)}");
                }
            }

            // Fall back to default
            if (DefaultSettings.TryGetValue(key, out var defaultValue))
            {
                if (defaultValue is T typedDefault)
                {
                    return typedDefault;
                }

                // Try to convert
                try
                {
                    return (T)Convert.ChangeType(defaultValue, typeof(T));
                }
                catch
                {
                    Debug.WriteLine($"Failed to convert default setting {key} from {defaultValue?.GetType()} to {typeof(T)}");
                }
            }

            // If all else fails, return default value for the type
            return default;
        }

        /// <summary>
        /// Set a setting value
        /// </summary>
        public void SetSetting<T>(string key, T value)
        {
            // Check if changed
            if (_settingsCache.TryGetValue(key, out var existingValue) &&
                existingValue != null && existingValue.Equals(value))
            {
                return; // No change
            }

            // Update the cache
            _settingsCache[key] = value;
            _isDirty = true;

            // Save the settings
            SaveSettings();
        }

        /// <summary>
        /// Reset a setting to its default value
        /// </summary>
        public void ResetSetting(string key)
        {
            if (DefaultSettings.TryGetValue(key, out var defaultValue))
            {
                _settingsCache[key] = defaultValue;
                _isDirty = true;
                SaveSettings();
            }
            else
            {
                _settingsCache.Remove(key);
                _isDirty = true;
                SaveSettings();
            }
        }

        /// <summary>
        /// Reset all settings to their default values
        /// </summary>
        public void ResetAllSettings()
        {
            _settingsCache.Clear();
            foreach (var kvp in DefaultSettings)
            {
                _settingsCache[kvp.Key] = kvp.Value;
            }
            _isDirty = true;
            SaveSettings();
        }

        /// <summary>
        /// Save settings to file
        /// </summary>
        public void SaveSettings()
        {
            if (!_isDirty) return;

            try
            {
                // Create an XML document
                var doc = new XDocument(
                    new XElement("Settings",
                        new XElement("Version", "1.0")));

                // Add settings
                var rootElement = doc.Root;
                foreach (var kvp in _settingsCache)
                {
                    AddSettingToXml(rootElement, kvp.Key, kvp.Value);
                }

                // Save the document
                doc.Save(_settingsFilePath);

                // Reset dirty flag
                _isDirty = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Load settings from file
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                // Check if settings file exists
                if (!File.Exists(_settingsFilePath))
                {
                    // Load default settings
                    foreach (var kvp in DefaultSettings)
                    {
                        _settingsCache[kvp.Key] = kvp.Value;
                    }

                    // Save defaults to file
                    SaveSettings();
                    return;
                }

                // Load settings from file
                var doc = XDocument.Load(_settingsFilePath);
                if (doc.Root == null)
                {
                    throw new Exception("Invalid settings file format: no root element");
                }

                // Get all setting elements
                foreach (var element in doc.Root.Elements())
                {
                    if (element.Name == "Version") continue; // Skip version

                    // Get key and value
                    var key = element.Name.LocalName;
                    var typeAttr = element.Attribute("type");
                    var value = element.Value;

                    // Add to cache
                    _settingsCache[key] = ConvertFromString(value, typeAttr?.Value);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load settings: {ex.Message}");

                // Load default settings
                foreach (var kvp in DefaultSettings)
                {
                    _settingsCache[kvp.Key] = kvp.Value;
                }
            }
        }

        /// <summary>
        /// Add a setting to an XML element
        /// </summary>
        private void AddSettingToXml(XElement parent, string key, object value)
        {
            var element = new XElement(key);

            if (value == null)
            {
                element.SetAttributeValue("type", "null");
            }
            else
            {
                var type = value.GetType();
                element.SetAttributeValue("type", type.Name);
                element.Value = ConvertToString(value);
            }

            parent.Add(element);
        }

        /// <summary>
        /// Convert a value to a string representation
        /// </summary>
        private string ConvertToString(object value)
        {
            if (value == null) return string.Empty;

            if (value is bool b)
            {
                return b ? "true" : "false";
            }

            if (value is DateTime dt)
            {
                return dt.ToString("o"); // ISO 8601 format
            }

            if (value is IEnumerable<int> intList)
            {
                return string.Join(",", intList);
            }

            return value.ToString();
        }

        /// <summary>
        /// Convert a string to a typed value
        /// </summary>
        private object ConvertFromString(string value, string type)
        {
            if (type == "null" || string.IsNullOrEmpty(value))
            {
                return null;
            }

            switch (type)
            {
                case "Boolean":
                case "bool":
                    return value.Equals("true", StringComparison.OrdinalIgnoreCase);

                case "Int32":
                case "int":
                    return int.TryParse(value, out var intResult) ? intResult : 0;

                case "Double":
                case "double":
                    return double.TryParse(value, out var doubleResult) ? doubleResult : 0.0;

                case "DateTime":
                    return DateTime.TryParse(value, out var dateResult) ? dateResult : DateTime.MinValue;

                case "List`1":
                case "IEnumerable`1":
                    if (value.Contains(","))
                    {
                        var items = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        var list = new List<int>();
                        foreach (var item in items)
                        {
                            if (int.TryParse(item, out var itemValue))
                            {
                                list.Add(itemValue);
                            }
                        }
                        return list;
                    }
                    return new List<int>();

                default:
                    return value; // Default to string
            }
        }
    }
}