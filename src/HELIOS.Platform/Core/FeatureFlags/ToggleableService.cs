using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HELIOS.Platform.Core.FeatureFlags.Models;

namespace HELIOS.Platform.Core.FeatureFlags
{
    /// <summary>
    /// Unified toggleables service managing all toggleable features
    /// </summary>
    public class ToggleableService
    {
        private readonly FeatureFlagManager _featureFlagManager;
        private readonly SettingsStore _settingsStore;
        private readonly Dictionary<string, ToggleableCategory> _categories;

        public ToggleableService()
        {
            _featureFlagManager = new FeatureFlagManager();
            _settingsStore = new SettingsStore();
            _categories = new Dictionary<string, ToggleableCategory>();
            InitializeDefaultCategories();
        }

        /// <summary>
        /// Initialize default toggleable categories
        /// </summary>
        private void InitializeDefaultCategories()
        {
            var categories = new[]
            {
                new ToggleableCategory { Id = "Features", Name = "Features", Description = "Core features" },
                new ToggleableCategory { Id = "Beta", Name = "Beta Features", Description = "Beta/experimental features" },
                new ToggleableCategory { Id = "Performance", Name = "Performance", Description = "Performance tuning" },
                new ToggleableCategory { Id = "Analytics", Name = "Analytics", Description = "Analytics and telemetry" },
                new ToggleableCategory { Id = "Integration", Name = "Integrations", Description = "Third-party integrations" },
                new ToggleableCategory { Id = "Security", Name = "Security", Description = "Security features" },
                new ToggleableCategory { Id = "UI", Name = "User Interface", Description = "UI components" },
                new ToggleableCategory { Id = "Advanced", Name = "Advanced", Description = "Advanced settings" }
            };

            foreach (var category in categories)
            {
                _categories[category.Id] = category;
            }
        }

        /// <summary>
        /// Get or create a toggleable feature
        /// </summary>
        public async Task<Toggleable> GetOrCreateToggleableAsync(string toggleableId, string name, 
            string category = "Features", bool defaultState = false)
        {
            var toggle = new Toggleable
            {
                Id = toggleableId,
                Name = name,
                Category = category,
                IsEnabled = defaultState,
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow
            };

            return await Task.FromResult(toggle);
        }

        /// <summary>
        /// Toggle a feature on/off
        /// </summary>
        public async Task<bool> ToggleFeatureAsync(string toggleableId, bool state, string userId = null)
        {
            try
            {
                var flag = await _featureFlagManager.GetFlagAsync(toggleableId);
                
                if (flag == null)
                {
                    flag = new FeatureFlag
                    {
                        Id = toggleableId,
                        Name = toggleableId,
                        Enabled = state,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _featureFlagManager.CreateFlagAsync(flag);
                }
                else
                {
                    if (state)
                        await _featureFlagManager.EnableFlagAsync(toggleableId, userId);
                    else
                        await _featureFlagManager.DisableFlagAsync(toggleableId, userId);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if a feature is enabled
        /// </summary>
        public async Task<bool> IsEnabledAsync(string toggleableId, FeatureFlagContext context = null)
        {
            var result = await _featureFlagManager.EvaluateAsync(toggleableId, context);
            return result.Enabled;
        }

        /// <summary>
        /// Get all toggleables for a category
        /// </summary>
        public async Task<List<Toggleable>> GetToggleablesByCategoryAsync(string category)
        {
            var flags = await _featureFlagManager.GetFlagsByCategoryAsync(category);
            var toggleables = new List<Toggleable>();

            foreach (var flag in flags)
            {
                toggleables.Add(new Toggleable
                {
                    Id = flag.Id,
                    Name = flag.Name,
                    Description = flag.Description,
                    Category = flag.Category,
                    IsEnabled = flag.Enabled,
                    CreatedAt = flag.CreatedAt,
                    Tags = flag.Tags
                });
            }

            return toggleables;
        }

        /// <summary>
        /// Get all categories
        /// </summary>
        public List<ToggleableCategory> GetCategories()
        {
            return new List<ToggleableCategory>(_categories.Values);
        }

        /// <summary>
        /// Get performance tuning toggles
        /// </summary>
        public async Task<PerformanceTuning> GetPerformanceTuningAsync()
        {
            var tuning = new PerformanceTuning();

            var enableCaching = await _settingsStore.GetGlobalSettingAsync("performance.enable_caching");
            tuning.EnableCaching = enableCaching?.Value as bool? ?? true;

            var cacheTimeout = await _settingsStore.GetGlobalSettingAsync("performance.cache_timeout_seconds");
            tuning.CacheTimeoutSeconds = cacheTimeout?.Value as int? ?? 300;

            var enableCompression = await _settingsStore.GetGlobalSettingAsync("performance.enable_compression");
            tuning.EnableCompression = enableCompression?.Value as bool? ?? true;

            var maxConnections = await _settingsStore.GetGlobalSettingAsync("performance.max_connections");
            tuning.MaxConnections = maxConnections?.Value as int? ?? 100;

            return tuning;
        }

        /// <summary>
        /// Set performance tuning
        /// </summary>
        public async Task SetPerformanceTuningAsync(PerformanceTuning tuning)
        {
            await _settingsStore.SetGlobalSettingAsync(new Setting
            {
                Key = "performance.enable_caching",
                Value = tuning.EnableCaching,
                Type = "boolean",
                Category = "Performance"
            });

            await _settingsStore.SetGlobalSettingAsync(new Setting
            {
                Key = "performance.cache_timeout_seconds",
                Value = tuning.CacheTimeoutSeconds,
                Type = "number",
                Category = "Performance"
            });

            await _settingsStore.SetGlobalSettingAsync(new Setting
            {
                Key = "performance.enable_compression",
                Value = tuning.EnableCompression,
                Type = "boolean",
                Category = "Performance"
            });

            await _settingsStore.SetGlobalSettingAsync(new Setting
            {
                Key = "performance.max_connections",
                Value = tuning.MaxConnections,
                Type = "number",
                Category = "Performance"
            });
        }

        /// <summary>
        /// Get analytics/telemetry toggles
        /// </summary>
        public async Task<AnalyticsToggle> GetAnalyticsAsync()
        {
            var analytics = new AnalyticsToggle();

            var enableAnalytics = await _settingsStore.GetGlobalSettingAsync("analytics.enabled");
            analytics.Enabled = enableAnalytics?.Value as bool? ?? true;

            var enableTelemetry = await _settingsStore.GetGlobalSettingAsync("analytics.telemetry_enabled");
            analytics.TelemetryEnabled = enableTelemetry?.Value as bool? ?? false;

            var trackingLevel = await _settingsStore.GetGlobalSettingAsync("analytics.tracking_level");
            analytics.TrackingLevel = trackingLevel?.Value as string ?? "basic";

            return analytics;
        }

        /// <summary>
        /// Set analytics toggles
        /// </summary>
        public async Task SetAnalyticsAsync(AnalyticsToggle analytics)
        {
            await _settingsStore.SetGlobalSettingAsync(new Setting
            {
                Key = "analytics.enabled",
                Value = analytics.Enabled,
                Type = "boolean",
                Category = "Analytics",
                IsSecret = false
            });

            await _settingsStore.SetGlobalSettingAsync(new Setting
            {
                Key = "analytics.telemetry_enabled",
                Value = analytics.TelemetryEnabled,
                Type = "boolean",
                Category = "Analytics",
                IsSecret = false
            });

            await _settingsStore.SetGlobalSettingAsync(new Setting
            {
                Key = "analytics.tracking_level",
                Value = analytics.TrackingLevel,
                Type = "string",
                Category = "Analytics"
            });
        }

        /// <summary>
        /// Get integration toggles
        /// </summary>
        public async Task<IntegrationToggles> GetIntegrationsAsync()
        {
            var integrations = new IntegrationToggles();

            var enableAzure = await _settingsStore.GetGlobalSettingAsync("integration.azure_enabled");
            integrations.AzureEnabled = enableAzure?.Value as bool? ?? false;

            var enableAI = await _settingsStore.GetGlobalSettingAsync("integration.ai_enabled");
            integrations.AiEnabled = enableAI?.Value as bool? ?? false;

            var enableCloud = await _settingsStore.GetGlobalSettingAsync("integration.cloud_sync_enabled");
            integrations.CloudSyncEnabled = enableCloud?.Value as bool? ?? false;

            return integrations;
        }

        /// <summary>
        /// Set integration toggles
        /// </summary>
        public async Task SetIntegrationsAsync(IntegrationToggles integrations)
        {
            await _settingsStore.SetGlobalSettingAsync(new Setting
            {
                Key = "integration.azure_enabled",
                Value = integrations.AzureEnabled,
                Type = "boolean",
                Category = "Integration"
            });

            await _settingsStore.SetGlobalSettingAsync(new Setting
            {
                Key = "integration.ai_enabled",
                Value = integrations.AiEnabled,
                Type = "boolean",
                Category = "Integration"
            });

            await _settingsStore.SetGlobalSettingAsync(new Setting
            {
                Key = "integration.cloud_sync_enabled",
                Value = integrations.CloudSyncEnabled,
                Type = "boolean",
                Category = "Integration"
            });
        }

        /// <summary>
        /// Expose feature flag manager for advanced operations
        /// </summary>
        public FeatureFlagManager GetFeatureFlagManager() => _featureFlagManager;

        /// <summary>
        /// Expose settings store for advanced operations
        /// </summary>
        public SettingsStore GetSettingsStore() => _settingsStore;
    }

    /// <summary>
    /// Toggleable feature
    /// </summary>
    public class Toggleable
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public bool IsEnabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// Toggleable category
    /// </summary>
    public class ToggleableCategory
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// Performance tuning settings
    /// </summary>
    public class PerformanceTuning
    {
        public bool EnableCaching { get; set; } = true;
        public int CacheTimeoutSeconds { get; set; } = 300;
        public bool EnableCompression { get; set; } = true;
        public int MaxConnections { get; set; } = 100;
    }

    /// <summary>
    /// Analytics toggle settings
    /// </summary>
    public class AnalyticsToggle
    {
        public bool Enabled { get; set; } = true;
        public bool TelemetryEnabled { get; set; }
        public string TrackingLevel { get; set; } = "basic";
    }

    /// <summary>
    /// Integration toggles
    /// </summary>
    public class IntegrationToggles
    {
        public bool AzureEnabled { get; set; }
        public bool AiEnabled { get; set; }
        public bool CloudSyncEnabled { get; set; }
    }
}
