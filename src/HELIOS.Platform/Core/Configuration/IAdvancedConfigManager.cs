using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Configuration;

public class ConfigurationProfile
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public Dictionary<string, object> Settings { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}

public class ConfigurationChangeHistory
{
    public int Id { get; set; }
    public string SettingKey { get; set; }
    public object OldValue { get; set; }
    public object NewValue { get; set; }
    public string ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
    public string Reason { get; set; }
}

public class ConfigurationValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> ValidatedSettings { get; set; } = new();
}

public interface IAdvancedConfigManager
{
    Task<ConfigurationProfile> CreateProfileAsync(string name, string description);
    Task<ConfigurationProfile> GetProfileAsync(string profileId);
    Task<List<ConfigurationProfile>> ListProfilesAsync();
    Task<ConfigurationProfile> UpdateProfileAsync(string profileId, Dictionary<string, object> settings);
    Task<bool> DeleteProfileAsync(string profileId);
    Task<ConfigurationProfile> ActivateProfileAsync(string profileId);
    Task<ConfigurationValidationResult> ValidateConfigurationAsync(Dictionary<string, object> settings);
    Task<List<ConfigurationChangeHistory>> GetChangeHistoryAsync(string profileId, int limit = 100);
    Task<bool> ExportProfileAsync(string profileId, string exportPath);
    Task<ConfigurationProfile> ImportProfileAsync(string importPath);
    Task<Dictionary<string, object>> GetActiveConfigurationAsync();
    Task<bool> ResetToDefaultsAsync();
}

public class AdvancedConfigManager : IAdvancedConfigManager
{
    private readonly List<ConfigurationProfile> _profiles = new();
    private readonly List<ConfigurationChangeHistory> _changeHistory = new();
    private ConfigurationProfile _activeProfile;
    private int _historyIdCounter = 1;

    private readonly Dictionary<string, object> _defaultSettings = new()
    {
        { "AutoBackup", true },
        { "AutoBackupInterval", 3600 },
        { "CloudSync", false },
        { "CompressionEnabled", true },
        { "EncryptionAlgorithm", "AES-256" },
        { "LogLevel", "Info" },
        { "MaxConcurrentOperations", 4 },
        { "PerformanceMode", "Balanced" },
        { "SecurityPolicy", "Standard" },
        { "TelemetryEnabled", false },
        { "UpdateCheckInterval", 86400 },
        { "MaxLogFileSize", 104857600 },
        { "RetentionDays", 30 }
    };

    public AdvancedConfigManager()
    {
        InitializeDefaultProfile();
    }

    private void InitializeDefaultProfile()
    {
        var defaultProfile = new ConfigurationProfile
        {
            Id = "default",
            Name = "Default Configuration",
            Description = "Default system configuration",
            Settings = new Dictionary<string, object>(_defaultSettings),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };
        _profiles.Add(defaultProfile);
        _activeProfile = defaultProfile;
    }

    public async Task<ConfigurationProfile> CreateProfileAsync(string name, string description)
    {
        var profileId = Guid.NewGuid().ToString();
        var profile = new ConfigurationProfile
        {
            Id = profileId,
            Name = name,
            Description = description,
            Settings = new Dictionary<string, object>(_defaultSettings),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = false
        };

        _profiles.Add(profile);
        return await Task.FromResult(profile);
    }

    public async Task<ConfigurationProfile> GetProfileAsync(string profileId)
    {
        var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
        return await Task.FromResult(profile);
    }

    public async Task<List<ConfigurationProfile>> ListProfilesAsync()
    {
        return await Task.FromResult(new List<ConfigurationProfile>(_profiles));
    }

    public async Task<ConfigurationProfile> UpdateProfileAsync(string profileId, Dictionary<string, object> settings)
    {
        var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile == null)
            return await Task.FromResult<ConfigurationProfile>(null);

        var validationResult = await ValidateConfigurationAsync(settings);
        if (!validationResult.IsValid)
            return await Task.FromResult<ConfigurationProfile>(null);

        foreach (var setting in settings)
        {
            var oldValue = profile.Settings.ContainsKey(setting.Key) ? profile.Settings[setting.Key] : null;
            profile.Settings[setting.Key] = setting.Value;

            _changeHistory.Add(new ConfigurationChangeHistory
            {
                Id = _historyIdCounter++,
                SettingKey = setting.Key,
                OldValue = oldValue,
                NewValue = setting.Value,
                ChangedBy = "System",
                ChangedAt = DateTime.UtcNow,
                Reason = "Configuration update"
            });
        }

        profile.UpdatedAt = DateTime.UtcNow;
        return await Task.FromResult(profile);
    }

    public async Task<bool> DeleteProfileAsync(string profileId)
    {
        if (profileId == "default")
            return await Task.FromResult(false);

        var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile == null)
            return await Task.FromResult(false);

        if (profile.IsActive)
            return await Task.FromResult(false);

        _profiles.Remove(profile);
        return await Task.FromResult(true);
    }

    public async Task<ConfigurationProfile> ActivateProfileAsync(string profileId)
    {
        var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile == null)
            return await Task.FromResult<ConfigurationProfile>(null);

        if (_activeProfile != null)
            _activeProfile.IsActive = false;

        profile.IsActive = true;
        _activeProfile = profile;

        _changeHistory.Add(new ConfigurationChangeHistory
        {
            Id = _historyIdCounter++,
            SettingKey = "ActiveProfile",
            OldValue = _activeProfile?.Id,
            NewValue = profileId,
            ChangedBy = "System",
            ChangedAt = DateTime.UtcNow,
            Reason = "Profile activation"
        });

        return await Task.FromResult(profile);
    }

    public async Task<ConfigurationValidationResult> ValidateConfigurationAsync(Dictionary<string, object> settings)
    {
        var result = new ConfigurationValidationResult { IsValid = true };
        var validatedSettings = new Dictionary<string, object>();

        foreach (var setting in settings)
        {
            try
            {
                switch (setting.Key.ToLower())
                {
                    case "autobackup":
                        if (setting.Value is bool)
                            validatedSettings[setting.Key] = setting.Value;
                        else
                            result.Errors.Add($"Invalid value for {setting.Key}: must be boolean");
                        break;

                    case "autobackupinterval":
                    case "updatecheckinterval":
                    case "maxlogfilesize":
                    case "retentiondays":
                    case "maxconcurrentoperations":
                        if (setting.Value is int intVal && intVal > 0)
                            validatedSettings[setting.Key] = intVal;
                        else
                            result.Errors.Add($"Invalid value for {setting.Key}: must be positive integer");
                        break;

                    case "performancemode":
                        var validModes = new[] { "Balanced", "Performance", "PowerSaver", "Custom" };
                        if (validModes.Contains(setting.Value?.ToString()))
                            validatedSettings[setting.Key] = setting.Value;
                        else
                            result.Errors.Add($"Invalid PerformanceMode: use {string.Join(", ", validModes)}");
                        break;

                    case "encryptionalgorithm":
                        var validAlgos = new[] { "AES-128", "AES-256", "ChaCha20" };
                        if (validAlgos.Contains(setting.Value?.ToString()))
                            validatedSettings[setting.Key] = setting.Value;
                        else
                            result.Errors.Add($"Invalid EncryptionAlgorithm: use {string.Join(", ", validAlgos)}");
                        break;

                    case "securitypolicy":
                        var validPolicies = new[] { "Minimal", "Standard", "Strict", "Custom" };
                        if (validPolicies.Contains(setting.Value?.ToString()))
                            validatedSettings[setting.Key] = setting.Value;
                        else
                            result.Errors.Add($"Invalid SecurityPolicy: use {string.Join(", ", validPolicies)}");
                        break;

                    case "loglevel":
                        var validLevels = new[] { "Debug", "Info", "Warning", "Error", "Critical" };
                        if (validLevels.Contains(setting.Value?.ToString()))
                            validatedSettings[setting.Key] = setting.Value;
                        else
                            result.Errors.Add($"Invalid LogLevel: use {string.Join(", ", validLevels)}");
                        break;

                    case "cloudsync":
                    case "compressionenabled":
                    case "telemetryenabled":
                        if (setting.Value is bool)
                            validatedSettings[setting.Key] = setting.Value;
                        else
                            result.Errors.Add($"Invalid value for {setting.Key}: must be boolean");
                        break;

                    default:
                        result.Warnings.Add($"Unknown setting key: {setting.Key}");
                        validatedSettings[setting.Key] = setting.Value;
                        break;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error validating {setting.Key}: {ex.Message}");
            }
        }

        result.IsValid = result.Errors.Count == 0;
        result.ValidatedSettings = validatedSettings;

        return await Task.FromResult(result);
    }

    public async Task<List<ConfigurationChangeHistory>> GetChangeHistoryAsync(string profileId, int limit = 100)
    {
        var history = _changeHistory
            .OrderByDescending(h => h.ChangedAt)
            .Take(limit)
            .ToList();

        return await Task.FromResult(history);
    }

    public async Task<bool> ExportProfileAsync(string profileId, string exportPath)
    {
        var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile == null)
            return await Task.FromResult(false);

        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(profile, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(exportPath, json);
            return await Task.FromResult(true);
        }
        catch
        {
            return await Task.FromResult(false);
        }
    }

    public async Task<ConfigurationProfile> ImportProfileAsync(string importPath)
    {
        try
        {
            var json = await System.IO.File.ReadAllTextAsync(importPath);
            var profile = System.Text.Json.JsonSerializer.Deserialize<ConfigurationProfile>(json);
            if (profile == null)
                return await Task.FromResult<ConfigurationProfile>(null);

            profile.Id = Guid.NewGuid().ToString();
            profile.CreatedAt = DateTime.UtcNow;
            profile.UpdatedAt = DateTime.UtcNow;
            profile.IsActive = false;

            _profiles.Add(profile);
            return await Task.FromResult(profile);
        }
        catch
        {
            return await Task.FromResult<ConfigurationProfile>(null);
        }
    }

    public async Task<Dictionary<string, object>> GetActiveConfigurationAsync()
    {
        return await Task.FromResult(new Dictionary<string, object>(_activeProfile?.Settings ?? _defaultSettings));
    }

    public async Task<bool> ResetToDefaultsAsync()
    {
        if (_activeProfile != null)
        {
            _activeProfile.Settings = new Dictionary<string, object>(_defaultSettings);
            _activeProfile.UpdatedAt = DateTime.UtcNow;

            _changeHistory.Add(new ConfigurationChangeHistory
            {
                Id = _historyIdCounter++,
                SettingKey = "AllSettings",
                OldValue = "Previous",
                NewValue = "DefaultsRestored",
                ChangedBy = "System",
                ChangedAt = DateTime.UtcNow,
                Reason = "Reset to defaults"
            });
        }

        return await Task.FromResult(true);
    }
}
