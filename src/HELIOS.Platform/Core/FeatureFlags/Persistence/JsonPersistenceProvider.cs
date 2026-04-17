using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HELIOS.Platform.Core.FeatureFlags.Models;

namespace HELIOS.Platform.Core.FeatureFlags.Persistence
{
    /// <summary>
    /// JSON-based persistence provider
    /// </summary>
    public class JsonPersistenceProvider : IFeatureFlagsPersistenceProvider
    {
        private readonly string _basePath;
        private readonly JsonSerializerOptions _jsonOptions;

        public JsonPersistenceProvider(string basePath)
        {
            _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
            
            if (!Directory.Exists(_basePath))
                Directory.CreateDirectory(_basePath);

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        /// <summary>
        /// Save feature flags to persistent storage
        /// </summary>
        public async Task SaveFeatureFlagsAsync(List<FeatureFlag> flags)
        {
            try
            {
                string filePath = Path.Combine(_basePath, "feature-flags.json");
                var json = JsonSerializer.Serialize(flags, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                throw new PersistenceException("Failed to save feature flags", ex);
            }
        }

        /// <summary>
        /// Load feature flags from persistent storage
        /// </summary>
        public async Task<List<FeatureFlag>> LoadFeatureFlagsAsync()
        {
            try
            {
                string filePath = Path.Combine(_basePath, "feature-flags.json");
                
                if (!File.Exists(filePath))
                    return new List<FeatureFlag>();

                var json = await File.ReadAllTextAsync(filePath);
                var flags = JsonSerializer.Deserialize<List<FeatureFlag>>(json, _jsonOptions) ?? new List<FeatureFlag>();
                return flags;
            }
            catch (Exception ex)
            {
                throw new PersistenceException("Failed to load feature flags", ex);
            }
        }

        /// <summary>
        /// Save settings to persistent storage
        /// </summary>
        public async Task SaveSettingsAsync(Dictionary<string, Setting> globalSettings, 
            Dictionary<string, Dictionary<string, Setting>> userSettings)
        {
            try
            {
                string globalPath = Path.Combine(_basePath, "settings-global.json");
                var globalJson = JsonSerializer.Serialize(globalSettings, _jsonOptions);
                await File.WriteAllTextAsync(globalPath, globalJson);

                string userPath = Path.Combine(_basePath, "settings-user.json");
                var userJson = JsonSerializer.Serialize(userSettings, _jsonOptions);
                await File.WriteAllTextAsync(userPath, userJson);
            }
            catch (Exception ex)
            {
                throw new PersistenceException("Failed to save settings", ex);
            }
        }

        /// <summary>
        /// Load settings from persistent storage
        /// </summary>
        public async Task<(Dictionary<string, Setting> Global, Dictionary<string, Dictionary<string, Setting>> User)> 
            LoadSettingsAsync()
        {
            try
            {
                var globalSettings = new Dictionary<string, Setting>();
                var userSettings = new Dictionary<string, Dictionary<string, Setting>>();

                string globalPath = Path.Combine(_basePath, "settings-global.json");
                if (File.Exists(globalPath))
                {
                    var json = await File.ReadAllTextAsync(globalPath);
                    globalSettings = JsonSerializer.Deserialize<Dictionary<string, Setting>>(json, _jsonOptions) 
                        ?? new Dictionary<string, Setting>();
                }

                string userPath = Path.Combine(_basePath, "settings-user.json");
                if (File.Exists(userPath))
                {
                    var json = await File.ReadAllTextAsync(userPath);
                    userSettings = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Setting>>>(json, _jsonOptions)
                        ?? new Dictionary<string, Dictionary<string, Setting>>();
                }

                return (globalSettings, userSettings);
            }
            catch (Exception ex)
            {
                throw new PersistenceException("Failed to load settings", ex);
            }
        }

        /// <summary>
        /// Save backup
        /// </summary>
        public async Task SaveBackupAsync(SettingsBackup backup)
        {
            try
            {
                string filePath = Path.Combine(_basePath, "backups", $"backup-{backup.BackupId}.json");
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                var json = JsonSerializer.Serialize(backup, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                throw new PersistenceException("Failed to save backup", ex);
            }
        }

        /// <summary>
        /// Load backup
        /// </summary>
        public async Task<SettingsBackup> LoadBackupAsync(string backupId)
        {
            try
            {
                string filePath = Path.Combine(_basePath, "backups", $"backup-{backupId}.json");
                
                if (!File.Exists(filePath))
                    return null;

                var json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<SettingsBackup>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                throw new PersistenceException("Failed to load backup", ex);
            }
        }

        /// <summary>
        /// List available backups
        /// </summary>
        public async Task<List<SettingsBackup>> ListBackupsAsync()
        {
            try
            {
                string backupDir = Path.Combine(_basePath, "backups");
                var backups = new List<SettingsBackup>();

                if (!Directory.Exists(backupDir))
                    return backups;

                foreach (var file in Directory.GetFiles(backupDir, "backup-*.json"))
                {
                    var json = await File.ReadAllTextAsync(file);
                    var backup = JsonSerializer.Deserialize<SettingsBackup>(json, _jsonOptions);
                    if (backup != null)
                        backups.Add(backup);
                }

                return backups;
            }
            catch (Exception ex)
            {
                throw new PersistenceException("Failed to list backups", ex);
            }
        }
    }

    /// <summary>
    /// Persistence provider interface
    /// </summary>
    public interface IFeatureFlagsPersistenceProvider
    {
        Task SaveFeatureFlagsAsync(List<FeatureFlag> flags);
        Task<List<FeatureFlag>> LoadFeatureFlagsAsync();
        Task SaveSettingsAsync(Dictionary<string, Setting> globalSettings, 
            Dictionary<string, Dictionary<string, Setting>> userSettings);
        Task<(Dictionary<string, Setting> Global, Dictionary<string, Dictionary<string, Setting>> User)> LoadSettingsAsync();
        Task SaveBackupAsync(SettingsBackup backup);
        Task<SettingsBackup> LoadBackupAsync(string backupId);
        Task<List<SettingsBackup>> ListBackupsAsync();
    }

    /// <summary>
    /// Persistence exception
    /// </summary>
    public class PersistenceException : Exception
    {
        public PersistenceException(string message) : base(message) { }
        public PersistenceException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}
