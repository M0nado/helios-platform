using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HELIOS.Platform.Core.FeatureFlags.Models;

namespace HELIOS.Platform.Core.FeatureFlags
{
    /// <summary>
    /// Settings store for managing user and global settings with validation
    /// </summary>
    public class SettingsStore
    {
        private readonly Dictionary<string, Setting> _globalSettings;
        private readonly Dictionary<string, Dictionary<string, Setting>> _userSettings;
        private readonly Dictionary<string, SettingsSchema> _schemas;
        private readonly Dictionary<string, UserPreferences> _userPreferences;
        private readonly GlobalSettings _globalSettingsContainer;
        private readonly List<SettingsAuditLog> _auditLog;
        private readonly object _lockObject = new object();

        public SettingsStore()
        {
            _globalSettings = new Dictionary<string, Setting>();
            _userSettings = new Dictionary<string, Dictionary<string, Setting>>();
            _schemas = new Dictionary<string, SettingsSchema>();
            _userPreferences = new Dictionary<string, UserPreferences>();
            _globalSettingsContainer = new GlobalSettings { GlobalSettingsId = Guid.NewGuid().ToString() };
            _auditLog = new List<SettingsAuditLog>();
        }

        /// <summary>
        /// Register a settings schema
        /// </summary>
        public async Task RegisterSchemaAsync(SettingsSchema schema)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            if (string.IsNullOrEmpty(schema.SchemaId)) schema.SchemaId = Guid.NewGuid().ToString();

            lock (_lockObject)
            {
                _schemas[schema.SchemaId] = schema;
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Set a global setting
        /// </summary>
        public async Task<ValidationResult> SetGlobalSettingAsync(Setting setting, string userId = null)
        {
            if (setting == null) return new ValidationResult(false) 
            { 
                Errors = new List<ValidationError> { new() { ErrorMessage = "Setting cannot be null" } }
            };

            var validationResult = await ValidateSettingAsync(setting);
            if (!validationResult.IsValid)
                return validationResult;

            if (string.IsNullOrEmpty(setting.SettingId))
                setting.SettingId = Guid.NewGuid().ToString();

            lock (_lockObject)
            {
                var oldValue = _globalSettings.ContainsKey(setting.Key) 
                    ? _globalSettings[setting.Key].Value 
                    : null;

                setting.IsGlobal = true;
                setting.LastModifiedAt = DateTime.UtcNow;
                setting.Version++;

                _globalSettings[setting.Key] = setting;

                LogAudit(new SettingsAuditLog
                {
                    AuditId = Guid.NewGuid().ToString(),
                    SettingKey = setting.Key,
                    UserId = userId ?? "SYSTEM",
                    Action = "Update",
                    OldValue = oldValue,
                    NewValue = setting.Value,
                    Timestamp = DateTime.UtcNow
                });
            }

            return await Task.FromResult(validationResult);
        }

        /// <summary>
        /// Set a user setting
        /// </summary>
        public async Task<ValidationResult> SetUserSettingAsync(Setting setting, string userId, string tenantId = null)
        {
            if (setting == null) return new ValidationResult(false)
            {
                Errors = new List<ValidationError> { new() { ErrorMessage = "Setting cannot be null" } }
            };

            if (string.IsNullOrEmpty(userId))
                return new ValidationResult(false)
                {
                    Errors = new List<ValidationError> { new() { ErrorMessage = "User ID is required" } }
                };

            var validationResult = await ValidateSettingAsync(setting);
            if (!validationResult.IsValid)
                return validationResult;

            if (string.IsNullOrEmpty(setting.SettingId))
                setting.SettingId = Guid.NewGuid().ToString();

            lock (_lockObject)
            {
                if (!_userSettings.ContainsKey(userId))
                    _userSettings[userId] = new Dictionary<string, Setting>();

                var oldValue = _userSettings[userId].ContainsKey(setting.Key)
                    ? _userSettings[userId][setting.Key].Value
                    : null;

                setting.IsGlobal = false;
                setting.UserId = userId;
                setting.TenantId = tenantId;
                setting.LastModifiedAt = DateTime.UtcNow;
                setting.Version++;

                _userSettings[userId][setting.Key] = setting;

                LogAudit(new SettingsAuditLog
                {
                    AuditId = Guid.NewGuid().ToString(),
                    SettingKey = setting.Key,
                    UserId = userId,
                    Action = "Update",
                    OldValue = oldValue,
                    NewValue = setting.Value,
                    Timestamp = DateTime.UtcNow
                });
            }

            return await Task.FromResult(validationResult);
        }

        /// <summary>
        /// Get a global setting
        /// </summary>
        public async Task<Setting> GetGlobalSettingAsync(string key)
        {
            lock (_lockObject)
            {
                _globalSettings.TryGetValue(key, out var setting);
                return Task.FromResult(setting).Result;
            }
        }

        /// <summary>
        /// Get a user setting with fallback to global
        /// </summary>
        public async Task<Setting> GetUserSettingAsync(string key, string userId)
        {
            lock (_lockObject)
            {
                if (_userSettings.TryGetValue(userId, out var userSettings) &&
                    userSettings.TryGetValue(key, out var setting))
                {
                    return Task.FromResult(setting).Result;
                }

                // Fallback to global setting
                _globalSettings.TryGetValue(key, out var globalSetting);
                return Task.FromResult(globalSetting).Result;
            }
        }

        /// <summary>
        /// Get all global settings
        /// </summary>
        public async Task<Dictionary<string, Setting>> GetAllGlobalSettingsAsync()
        {
            lock (_lockObject)
            {
                return Task.FromResult(new Dictionary<string, Setting>(_globalSettings)).Result;
            }
        }

        /// <summary>
        /// Get all user settings
        /// </summary>
        public async Task<Dictionary<string, Setting>> GetAllUserSettingsAsync(string userId)
        {
            lock (_lockObject)
            {
                if (_userSettings.TryGetValue(userId, out var settings))
                    return Task.FromResult(new Dictionary<string, Setting>(settings)).Result;

                return Task.FromResult(new Dictionary<string, Setting>()).Result;
            }
        }

        /// <summary>
        /// Delete a setting
        /// </summary>
        public async Task<bool> DeleteSettingAsync(string key, string userId = null)
        {
            lock (_lockObject)
            {
                bool deleted;

                if (userId == null)
                {
                    deleted = _globalSettings.Remove(key);
                }
                else
                {
                    if (_userSettings.TryGetValue(userId, out var settings))
                        deleted = settings.Remove(key);
                    else
                        deleted = false;
                }

                if (deleted)
                {
                    LogAudit(new SettingsAuditLog
                    {
                        AuditId = Guid.NewGuid().ToString(),
                        SettingKey = key,
                        UserId = userId ?? "SYSTEM",
                        Action = "Delete",
                        Timestamp = DateTime.UtcNow
                    });
                }

                return Task.FromResult(deleted).Result;
            }
        }

        /// <summary>
        /// Validate a setting
        /// </summary>
        public async Task<ValidationResult> ValidateSettingAsync(Setting setting)
        {
            var result = new ValidationResult();
            var errors = new List<ValidationError>();

            if (string.IsNullOrEmpty(setting.Key))
            {
                errors.Add(new ValidationError
                {
                    PropertyName = nameof(setting.Key),
                    ErrorCode = "REQUIRED",
                    ErrorMessage = "Setting key is required"
                });
            }

            if (!string.IsNullOrEmpty(setting.Type))
            {
                switch (setting.Type.ToLower())
                {
                    case "number":
                        if (setting.Value != null && !IsNumeric(setting.Value))
                            errors.Add(new ValidationError
                            {
                                PropertyName = nameof(setting.Value),
                                ErrorCode = "INVALID_TYPE",
                                ErrorMessage = $"Value must be numeric, got {setting.Value.GetType().Name}"
                            });
                        break;
                    case "boolean":
                        if (setting.Value != null && !(setting.Value is bool))
                            errors.Add(new ValidationError
                            {
                                PropertyName = nameof(setting.Value),
                                ErrorCode = "INVALID_TYPE",
                                ErrorMessage = "Value must be boolean"
                            });
                        break;
                }
            }

            result.IsValid = errors.Count == 0;
            result.Errors = errors;

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Set user preferences
        /// </summary>
        public async Task SetUserPreferencesAsync(UserPreferences preferences)
        {
            if (preferences == null) throw new ArgumentNullException(nameof(preferences));

            lock (_lockObject)
            {
                preferences.LastModifiedAt = DateTime.UtcNow;
                _userPreferences[preferences.UserId] = preferences;
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Get user preferences
        /// </summary>
        public async Task<UserPreferences> GetUserPreferencesAsync(string userId)
        {
            lock (_lockObject)
            {
                if (_userPreferences.TryGetValue(userId, out var preferences))
                    return Task.FromResult(preferences).Result;

                return Task.FromResult(new UserPreferences { UserId = userId }).Result;
            }
        }

        /// <summary>
        /// Get audit log
        /// </summary>
        public async Task<List<SettingsAuditLog>> GetAuditLogAsync(string settingKey = null, int limit = 1000)
        {
            lock (_lockObject)
            {
                var logs = _auditLog
                    .Where(l => settingKey == null || l.SettingKey == settingKey)
                    .OrderByDescending(l => l.Timestamp)
                    .Take(limit)
                    .ToList();

                return Task.FromResult(logs).Result;
            }
        }

        /// <summary>
        /// Log audit entry
        /// </summary>
        private void LogAudit(SettingsAuditLog log)
        {
            _auditLog.Add(log);
            if (_auditLog.Count > 10000)
                _auditLog.RemoveRange(0, 5000);
        }

        /// <summary>
        /// Check if value is numeric
        /// </summary>
        private bool IsNumeric(object value)
        {
            return value is int or long or float or double or decimal;
        }
    }
}
