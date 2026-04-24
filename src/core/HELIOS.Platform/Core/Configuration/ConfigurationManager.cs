using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;

namespace HELIOS.Platform.Core.Configuration
{
    /// <summary>
    /// Application configuration and settings management.
    /// </summary>
    public class ConfigurationManager
    {
        private readonly Core.Logging.ILogger _logger;
        private readonly Dictionary<string, object> _settings = new();
        private readonly string _configPath;

        public ConfigurationManager()
        {
            _logger = ServiceContainer.Instance.GetService<Core.Logging.ILogger>();
            _configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HELIOS");
            
            EnsureConfigDirectory();
            LoadSettings();
        }

        /// <summary>
        /// Ensure configuration directory exists.
        /// </summary>
        private void EnsureConfigDirectory()
        {
            try
            {
                if (!Directory.Exists(_configPath))
                {
                    Directory.CreateDirectory(_configPath);
                    _logger?.Debug($"Created configuration directory: {_configPath}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error("Failed to create configuration directory", ex);
            }
        }

        /// <summary>
        /// Load settings from disk.
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                var settingsFile = Path.Combine(_configPath, "settings.json");
                if (File.Exists(settingsFile))
                {
                    var json = File.ReadAllText(settingsFile);
                    var loaded = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    if (loaded != null)
                    {
                        _settings.Clear();
                        foreach (var kv in loaded)
                        {
                            _settings[kv.Key] = kv.Value;
                        }
                        _logger?.Debug($"Loaded {_settings.Count} settings");
                    }
                }
                else
                {
                    LoadDefaults();
                }
            }
            catch (Exception ex)
            {
                _logger?.Error("Failed to load settings", ex);
                LoadDefaults();
            }
        }

        /// <summary>
        /// Load default settings.
        /// </summary>
        private void LoadDefaults()
        {
            _settings["theme"] = "dark";
            _settings["autoStart"] = false;
            _settings["checkUpdates"] = true;
            _settings["telemetry"] = true;
            _settings["language"] = "en-US";
            _logger?.Debug("Loaded default settings");
        }

        /// <summary>
        /// Get a setting value.
        /// </summary>
        public T GetSetting<T>(string key, T defaultValue = default)
        {
            try
            {
                if (_settings.TryGetValue(key, out var value))
                {
                    if (value is T typedValue)
                        return typedValue;
                    
                    // Try to convert
                    if (value is JsonElement element)
                        return JsonSerializer.Deserialize<T>(element.GetRawText());
                }
            }
            catch (Exception ex)
            {
                _logger?.Warning($"Failed to get setting '{key}': {ex.Message}");
            }

            return defaultValue;
        }

        /// <summary>
        /// Set a setting value.
        /// </summary>
        public async Task SetSettingAsync(string key, object value)
        {
            try
            {
                _settings[key] = value;
                await SaveSettingsAsync();
                _logger?.Debug($"Setting '{key}' updated to '{value}'");
            }
            catch (Exception ex)
            {
                _logger?.Error($"Failed to set setting '{key}'", ex);
                throw;
            }
        }

        /// <summary>
        /// Save settings to disk.
        /// </summary>
        public async Task SaveSettingsAsync()
        {
            try
            {
                var settingsFile = Path.Combine(_configPath, "settings.json");
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(settingsFile, json);
                _logger?.Debug("Settings saved");
            }
            catch (Exception ex)
            {
                _logger?.Error("Failed to save settings", ex);
                throw;
            }
        }

        /// <summary>
        /// Get all settings.
        /// </summary>
        public Dictionary<string, object> GetAllSettings() => new Dictionary<string, object>(_settings);

        /// <summary>
        /// Get configuration directory path.
        /// </summary>
        public string GetConfigPath() => _configPath;
    }
}
