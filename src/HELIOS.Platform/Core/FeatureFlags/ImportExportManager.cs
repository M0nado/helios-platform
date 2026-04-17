using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HELIOS.Platform.Core.FeatureFlags.Models;
using HELIOS.Platform.Core.FeatureFlags.Persistence;

namespace HELIOS.Platform.Core.FeatureFlags
{
    /// <summary>
    /// Import/Export manager for settings and feature flags
    /// </summary>
    public class ImportExportManager
    {
        private readonly IFeatureFlagsPersistenceProvider _persistenceProvider;
        private readonly JsonSerializerOptions _jsonOptions;

        public ImportExportManager(IFeatureFlagsPersistenceProvider persistenceProvider)
        {
            _persistenceProvider = persistenceProvider ?? throw new ArgumentNullException(nameof(persistenceProvider));
            
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Export settings to file
        /// </summary>
        public async Task<ExportResult> ExportSettingsAsync(
            Dictionary<string, Setting> globalSettings,
            Dictionary<string, Dictionary<string, Setting>> userSettings,
            List<FeatureFlag> featureFlags,
            string exportPath,
            ExportConfiguration config)
        {
            try
            {
                var export = new SettingsExport
                {
                    ExportId = Guid.NewGuid().ToString(),
                    ExportName = config.ExportName ?? $"export-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
                    ExportedAt = DateTime.UtcNow,
                    ExportedBy = config.ExportedBy ?? "SYSTEM",
                    Format = config.Format ?? "JSON",
                    Version = 1
                };

                // Filter by scope and categories
                if (config.ExportScope != "FeatureFlagsOnly")
                {
                    export.GlobalSettings = FilterSettings(globalSettings, config.IncludedCategories, config.IncludeSensitiveData);
                    export.UserSettings = FilterUserSettings(userSettings, config.IncludedCategories, config.IncludeSensitiveData);
                }

                if (config.ExportScope != "SettingsOnly")
                {
                    export.FeatureFlags = featureFlags.ToDictionary(f => f.Id, f => (object)f);
                }

                export.IncludedCategories = config.IncludedCategories;
                export.ExportScope = config.ExportScope;
                export.IncludeSensitiveData = config.IncludeSensitiveData;

                // Write to file based on format
                string content = config.Format.ToUpper() == "JSON"
                    ? JsonSerializer.Serialize(export, _jsonOptions)
                    : JsonSerializer.Serialize(export, _jsonOptions);

                await File.WriteAllTextAsync(exportPath, content);

                return new ExportResult
                {
                    Success = true,
                    ExportId = export.ExportId,
                    ItemsExported = export.GlobalSettings.Count + 
                                   export.UserSettings.Values.Sum(d => d.Count) + 
                                   export.FeatureFlags.Count,
                    ExportPath = exportPath,
                    Message = "Export successful"
                };
            }
            catch (Exception ex)
            {
                return new ExportResult
                {
                    Success = false,
                    Message = $"Export failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Import settings from file
        /// </summary>
        public async Task<ImportResult> ImportSettingsAsync(
            string importPath,
            ImportConfiguration config,
            Dictionary<string, Setting> existingGlobalSettings,
            Dictionary<string, Dictionary<string, Setting>> existingUserSettings,
            List<FeatureFlag> existingFeatureFlags)
        {
            var result = new ImportResult
            {
                ImportedAt = DateTime.UtcNow,
                Success = false
            };

            try
            {
                if (!File.Exists(importPath))
                {
                    result.Message = "Import file not found";
                    return result;
                }

                string content = await File.ReadAllTextAsync(importPath);
                var export = JsonSerializer.Deserialize<SettingsExport>(content, _jsonOptions);

                if (export == null)
                {
                    result.Message = "Failed to parse import file";
                    return result;
                }

                result.Details = new List<ImportResultItem>();

                // Import global settings
                if (config.ImportScope != "FeatureFlagsOnly" && export.GlobalSettings != null)
                {
                    foreach (var kvp in export.GlobalSettings)
                    {
                        try
                        {
                            var setting = kvp.Value as Setting ?? JsonSerializer.Deserialize<Setting>(
                                JsonSerializer.Serialize(kvp.Value), _jsonOptions);

                            if (config.OverwriteExisting || !existingGlobalSettings.ContainsKey(kvp.Key))
                            {
                                existingGlobalSettings[kvp.Key] = setting;
                                result.ItemsImported++;

                                result.Details.Add(new ImportResultItem
                                {
                                    Key = kvp.Key,
                                    Success = true,
                                    Status = "Imported"
                                });
                            }
                            else
                            {
                                result.ItemsSkipped++;
                                result.Details.Add(new ImportResultItem
                                {
                                    Key = kvp.Key,
                                    Success = false,
                                    Status = "Skipped",
                                    ErrorMessage = "Setting already exists and overwrite is disabled"
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            result.ItemsFailed++;
                            result.Details.Add(new ImportResultItem
                            {
                                Key = kvp.Key,
                                Success = false,
                                Status = "Failed",
                                ErrorMessage = ex.Message
                            });
                        }
                    }
                }

                // Import feature flags
                if (config.ImportScope != "SettingsOnly" && export.FeatureFlags != null)
                {
                    foreach (var kvp in export.FeatureFlags)
                    {
                        try
                        {
                            var flag = kvp.Value as FeatureFlag ?? JsonSerializer.Deserialize<FeatureFlag>(
                                JsonSerializer.Serialize(kvp.Value), _jsonOptions);

                            if (config.OverwriteExisting || !existingFeatureFlags.Any(f => f.Id == kvp.Key))
                            {
                                var existing = existingFeatureFlags.FirstOrDefault(f => f.Id == kvp.Key);
                                if (existing != null)
                                    existingFeatureFlags.Remove(existing);
                                existingFeatureFlags.Add(flag);

                                result.ItemsImported++;
                                result.Details.Add(new ImportResultItem
                                {
                                    Key = kvp.Key,
                                    Success = true,
                                    Status = "Imported"
                                });
                            }
                            else
                            {
                                result.ItemsSkipped++;
                                result.Details.Add(new ImportResultItem
                                {
                                    Key = kvp.Key,
                                    Success = false,
                                    Status = "Skipped"
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            result.ItemsFailed++;
                            result.Details.Add(new ImportResultItem
                            {
                                Key = kvp.Key,
                                Success = false,
                                Status = "Failed",
                                ErrorMessage = ex.Message
                            });
                        }
                    }
                }

                result.Success = result.ItemsFailed == 0;
                result.Message = $"Import complete: {result.ItemsImported} imported, {result.ItemsSkipped} skipped, {result.ItemsFailed} failed";

                return result;
            }
            catch (Exception ex)
            {
                result.Message = $"Import failed: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Filter settings by categories
        /// </summary>
        private Dictionary<string, object> FilterSettings(
            Dictionary<string, Setting> settings,
            List<string> categories,
            bool includeSensitive)
        {
            var filtered = new Dictionary<string, object>();

            foreach (var kvp in settings)
            {
                var setting = kvp.Value;

                if (!includeSensitive && setting.IsSecret)
                    continue;

                if (categories != null && categories.Count > 0 && !categories.Contains(setting.Category))
                    continue;

                filtered[kvp.Key] = setting;
            }

            return filtered;
        }

        /// <summary>
        /// Filter user settings by categories
        /// </summary>
        private Dictionary<string, Dictionary<string, object>> FilterUserSettings(
            Dictionary<string, Dictionary<string, Setting>> userSettings,
            List<string> categories,
            bool includeSensitive)
        {
            var filtered = new Dictionary<string, Dictionary<string, object>>();

            foreach (var userKvp in userSettings)
            {
                var filteredUserSettings = FilterSettings(userKvp.Value, categories, includeSensitive);
                if (filteredUserSettings.Count > 0)
                    filtered[userKvp.Key] = filteredUserSettings;
            }

            return filtered;
        }
    }

    /// <summary>
    /// Export configuration
    /// </summary>
    public class ExportConfiguration
    {
        public string ExportName { get; set; }
        public string ExportedBy { get; set; }
        public string Format { get; set; } = "JSON";
        public List<string> IncludedCategories { get; set; } = new();
        public string ExportScope { get; set; } = "All";
        public bool IncludeSensitiveData { get; set; }
    }

    /// <summary>
    /// Export result
    /// </summary>
    public class ExportResult
    {
        public bool Success { get; set; }
        public string ExportId { get; set; }
        public int ItemsExported { get; set; }
        public string ExportPath { get; set; }
        public string Message { get; set; }
    }
}
