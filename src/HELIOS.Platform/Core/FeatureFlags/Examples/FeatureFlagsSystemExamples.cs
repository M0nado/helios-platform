using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HELIOS.Platform.Core.FeatureFlags;
using HELIOS.Platform.Core.FeatureFlags.Models;
using HELIOS.Platform.Core.FeatureFlags.UI;

namespace HELIOS.Platform.Core.FeatureFlags.Examples
{
    /// <summary>
    /// Example usage of the Feature Flags and Settings System
    /// </summary>
    public class FeatureFlagsSystemExamples
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== HELIOS Platform Feature Flags & Settings System ===\n");

            // Initialize orchestrator
            var orchestrator = new FeatureFlagsAndSettingsOrchestrator("./data/flags-settings");
            await orchestrator.InitializeAsync();

            // Example 1: Feature Flags
            await Example1_FeatureFlags(orchestrator);

            // Example 2: Settings Management
            await Example2_SettingsManagement(orchestrator);

            // Example 3: User Preferences
            await Example3_UserPreferences(orchestrator);

            // Example 4: Toggleables Service
            await Example4_ToggleablesService(orchestrator);

            // Example 5: Backup & Restore
            await Example5_BackupRestore(orchestrator);

            // Example 6: Import/Export
            await Example6_ImportExport(orchestrator);

            // Example 7: UI Generation
            await Example7_UIGeneration(orchestrator);

            // Persist data
            await orchestrator.PersistAsync();
            
            Console.WriteLine("\n=== All examples completed successfully! ===");
        }

        /// <summary>
        /// Example 1: Creating and evaluating feature flags
        /// </summary>
        private static async Task Example1_FeatureFlags(FeatureFlagsAndSettingsOrchestrator orchestrator)
        {
            Console.WriteLine("\n--- Example 1: Feature Flags ---");

            var manager = orchestrator.GetFeatureFlagManager();

            // Create a basic feature flag
            var basicFlag = await manager.CreateFlagAsync(new FeatureFlag
            {
                Id = "new-dashboard",
                Name = "New Dashboard",
                Description = "New dashboard redesign",
                Type = FeatureFlagTypeEnum.Basic,
                State = FeatureFlagState.Enabled,
                Enabled = true,
                Category = "Features",
                Tags = new List<string> { "ui", "redesign" },
                Priority = 10
            });

            Console.WriteLine($"✓ Created feature flag: {basicFlag.Name}");

            // Create a percentage rollout flag
            var percentageFlag = await manager.CreateFlagAsync(new FeatureFlag
            {
                Id = "ai-recommendations",
                Name = "AI Recommendations",
                Description = "AI-powered product recommendations",
                Type = FeatureFlagTypeEnum.Percentage,
                RolloutPercentage = 25,
                Enabled = true,
                Category = "Beta",
                Tags = new List<string> { "ai", "beta" },
                Priority = 20
            });

            Console.WriteLine($"✓ Created percentage flag (25% rollout): {percentageFlag.Name}");

            // Create a beta-only flag
            var betaFlag = await manager.CreateFlagAsync(new FeatureFlag
            {
                Id = "advanced-analytics",
                Name = "Advanced Analytics",
                Description = "Advanced analytics dashboard",
                Type = FeatureFlagTypeEnum.Basic,
                State = FeatureFlagState.BetaOnly,
                Enabled = true,
                Category = "Beta",
                Tags = new List<string> { "analytics", "beta" }
            });

            Console.WriteLine($"✓ Created beta-only flag: {betaFlag.Name}");

            // Evaluate flags
            Console.WriteLine("\nEvaluating flags:");

            var regularContext = new FeatureFlagContext
            {
                UserId = "user123",
                TenantId = "tenant456",
                Environment = "production",
                IsBetaUser = false
            };

            var betaContext = new FeatureFlagContext
            {
                UserId = "beta-user",
                TenantId = "tenant456",
                Environment = "production",
                IsBetaUser = true
            };

            var basicResult = await manager.EvaluateAsync("new-dashboard", regularContext);
            Console.WriteLine($"  new-dashboard: {(basicResult.Enabled ? "✓ ENABLED" : "✗ DISABLED")}");

            var percentageResult1 = await manager.EvaluateAsync("ai-recommendations", regularContext);
            Console.WriteLine($"  ai-recommendations (user123): {(percentageResult1.Enabled ? "✓ ENABLED" : "✗ DISABLED")}");

            var betaResult = await manager.EvaluateAsync("advanced-analytics", betaContext);
            Console.WriteLine($"  advanced-analytics (beta user): {(betaResult.Enabled ? "✓ ENABLED" : "✗ DISABLED")}");

            // Get metrics
            var metrics = await manager.GetMetricsAsync("ai-recommendations");
            if (metrics != null)
            {
                Console.WriteLine($"\nMetrics for ai-recommendations:");
                Console.WriteLine($"  Total Evaluations: {metrics.TotalEvaluations}");
                Console.WriteLine($"  Enabled Count: {metrics.EnabledCount}");
            }
        }

        /// <summary>
        /// Example 2: Settings management
        /// </summary>
        private static async Task Example2_SettingsManagement(FeatureFlagsAndSettingsOrchestrator orchestrator)
        {
            Console.WriteLine("\n--- Example 2: Settings Management ---");

            var store = orchestrator.GetSettingsStore();

            // Register a schema
            var schema = new SettingsSchema
            {
                SchemaId = "app-settings",
                SchemaName = "Application Settings",
                Properties = new Dictionary<string, SettingProperty>
                {
                    ["max_concurrent_users"] = new SettingProperty
                    {
                        Name = "Maximum Concurrent Users",
                        Type = "number",
                        DefaultValue = 1000,
                        MinValue = 1,
                        MaxValue = 100000,
                        Category = "System"
                    },
                    ["enable_notifications"] = new SettingProperty
                    {
                        Name = "Enable Notifications",
                        Type = "boolean",
                        DefaultValue = true,
                        Category = "Feature"
                    },
                    ["api_timeout_seconds"] = new SettingProperty
                    {
                        Name = "API Timeout (seconds)",
                        Type = "number",
                        DefaultValue = 30,
                        MinValue = 5,
                        MaxValue = 300,
                        Category = "Performance"
                    }
                }
            };

            await store.RegisterSchemaAsync(schema);
            Console.WriteLine("✓ Registered settings schema");

            // Set global settings
            var maxUsers = new Setting
            {
                Key = "max_concurrent_users",
                Value = 5000,
                Type = "number",
                Category = "System"
            };

            var validationResult = await store.SetGlobalSettingAsync(maxUsers);
            Console.WriteLine($"✓ Set global setting (valid: {validationResult.IsValid})");

            // Set user-specific setting
            var userNotifications = new Setting
            {
                Key = "enable_notifications",
                Value = false,
                Type = "boolean",
                Category = "Feature"
            };

            await store.SetUserSettingAsync(userNotifications, "user123");
            Console.WriteLine("✓ Set user-specific setting");

            // Retrieve settings
            var retrievedSetting = await store.GetGlobalSettingAsync("max_concurrent_users");
            Console.WriteLine($"✓ Retrieved global setting: {retrievedSetting?.Key} = {retrievedSetting?.Value}");

            var userSetting = await store.GetUserSettingAsync("enable_notifications", "user123");
            Console.WriteLine($"✓ Retrieved user setting: {userSetting?.Key} = {userSetting?.Value}");

            // Get audit log
            var auditLogs = await store.GetAuditLogAsync(limit: 5);
            Console.WriteLine($"✓ Audit log entries: {auditLogs.Count}");
        }

        /// <summary>
        /// Example 3: User preferences
        /// </summary>
        private static async Task Example3_UserPreferences(FeatureFlagsAndSettingsOrchestrator orchestrator)
        {
            Console.WriteLine("\n--- Example 3: User Preferences ---");

            var store = orchestrator.GetSettingsStore();

            var preferences = new UserPreferences
            {
                PreferenceId = Guid.NewGuid().ToString(),
                UserId = "user123",
                UIPreferences = new Dictionary<string, object>
                {
                    ["theme"] = "dark",
                    ["sidebar_collapsed"] = true,
                    ["font_size"] = 14
                },
                NotificationPreferences = new Dictionary<string, object>
                {
                    ["email_notifications"] = true,
                    ["push_notifications"] = false,
                    ["sms_notifications"] = false
                },
                PrivacyPreferences = new Dictionary<string, object>
                {
                    ["share_analytics"] = false,
                    ["share_location"] = false
                },
                PerformancePreferences = new Dictionary<string, object>
                {
                    ["enable_animations"] = false,
                    ["reduce_data_usage"] = true
                }
            };

            await store.SetUserPreferencesAsync(preferences);
            Console.WriteLine("✓ Set user preferences");

            var retrievedPrefs = await store.GetUserPreferencesAsync("user123");
            Console.WriteLine($"✓ Retrieved user preferences (theme: {retrievedPrefs.UIPreferences["theme"]})");
        }

        /// <summary>
        /// Example 4: Toggleables service
        /// </summary>
        private static async Task Example4_ToggleablesService(FeatureFlagsAndSettingsOrchestrator orchestrator)
        {
            Console.WriteLine("\n--- Example 4: Toggleables Service ---");

            var service = orchestrator.GetToggleableService();

            // Performance tuning
            Console.WriteLine("\nPerformance Tuning:");
            var tuning = await service.GetPerformanceTuningAsync();
            Console.WriteLine($"  Caching enabled: {tuning.EnableCaching}");
            Console.WriteLine($"  Cache timeout: {tuning.CacheTimeoutSeconds}s");

            tuning.EnableCaching = true;
            tuning.CacheTimeoutSeconds = 600;
            await service.SetPerformanceTuningAsync(tuning);
            Console.WriteLine("  ✓ Updated performance settings");

            // Analytics
            Console.WriteLine("\nAnalytics:");
            var analytics = await service.GetAnalyticsAsync();
            Console.WriteLine($"  Analytics enabled: {analytics.Enabled}");
            Console.WriteLine($"  Telemetry enabled: {analytics.TelemetryEnabled}");

            analytics.Enabled = true;
            analytics.TrackingLevel = "comprehensive";
            await service.SetAnalyticsAsync(analytics);
            Console.WriteLine("  ✓ Updated analytics settings");

            // Integrations
            Console.WriteLine("\nIntegrations:");
            var integrations = await service.GetIntegrationsAsync();
            Console.WriteLine($"  Azure enabled: {integrations.AzureEnabled}");
            Console.WriteLine($"  AI enabled: {integrations.AiEnabled}");

            integrations.AzureEnabled = true;
            integrations.AiEnabled = true;
            await service.SetIntegrationsAsync(integrations);
            Console.WriteLine("  ✓ Updated integration settings");

            // Categories
            Console.WriteLine("\nToggleable Categories:");
            var categories = service.GetCategories();
            foreach (var category in categories)
            {
                Console.WriteLine($"  - {category.Name}");
            }
        }

        /// <summary>
        /// Example 5: Backup and restore
        /// </summary>
        private static async Task Example5_BackupRestore(FeatureFlagsAndSettingsOrchestrator orchestrator)
        {
            Console.WriteLine("\n--- Example 5: Backup & Restore ---");

            var manager = orchestrator.GetFeatureFlagManager();
            var backupManager = orchestrator.GetBackupRestoreManager();

            // Get current state
            var flags = await manager.GetAllFlagsAsync();
            var store = orchestrator.GetSettingsStore();
            var globalSettings = await store.GetAllGlobalSettingsAsync();

            // Create backup
            var backup = await backupManager.CreateFullBackupAsync(
                globalSettings,
                new Dictionary<string, Dictionary<string, Setting>>(),
                flags,
                $"backup-{DateTime.Now:yyyyMMdd-HHmmss}",
                "admin");

            Console.WriteLine($"✓ Created backup: {backup.BackupId}");
            Console.WriteLine($"  Size: {backup.SizeBytes} bytes");
            Console.WriteLine($"  Items: {backup.GlobalSettings.Count + backup.FeatureFlags.Count}");

            // List backups
            var backups = await backupManager.ListBackupsAsync();
            Console.WriteLine($"✓ Total backups: {backups.Count}");
        }

        /// <summary>
        /// Example 6: Import/Export
        /// </summary>
        private static async Task Example6_ImportExport(FeatureFlagsAndSettingsOrchestrator orchestrator)
        {
            Console.WriteLine("\n--- Example 6: Import/Export ---");

            var manager = orchestrator.GetFeatureFlagManager();
            var importExportManager = orchestrator.GetImportExportManager();

            var flags = await manager.GetAllFlagsAsync();
            var store = orchestrator.GetSettingsStore();
            var globalSettings = await store.GetAllGlobalSettingsAsync();

            // Export settings
            var exportResult = await importExportManager.ExportSettingsAsync(
                globalSettings,
                new Dictionary<string, Dictionary<string, Setting>>(),
                flags,
                "./exports/helios-config.json",
                new ExportConfiguration
                {
                    ExportName = "HELIOS Configuration Export",
                    Format = "JSON",
                    ExportScope = "All",
                    IncludeSensitiveData = false
                });

            Console.WriteLine($"✓ Exported settings: {exportResult.ItemsExported} items");
            Console.WriteLine($"  Location: {exportResult.ExportPath}");
        }

        /// <summary>
        /// Example 7: UI Generation
        /// </summary>
        private static async Task Example7_UIGeneration(FeatureFlagsAndSettingsOrchestrator orchestrator)
        {
            Console.WriteLine("\n--- Example 7: UI Generation ---");

            var uiGenerator = orchestrator.GetUIGenerator();
            var manager = orchestrator.GetFeatureFlagManager();

            // Create a schema
            var schema = new SettingsSchema
            {
                SchemaId = "ui-settings",
                SchemaName = "UI Settings",
                Properties = new Dictionary<string, SettingProperty>
                {
                    ["theme"] = new SettingProperty
                    {
                        Name = "Theme",
                        Type = "string",
                        AllowedValues = new List<object> { "light", "dark", "auto" },
                        DefaultValue = "auto"
                    },
                    ["font_size"] = new SettingProperty
                    {
                        Name = "Font Size",
                        Type = "number",
                        MinValue = 8,
                        MaxValue = 24,
                        DefaultValue = 14
                    },
                    ["enable_animations"] = new SettingProperty
                    {
                        Name = "Enable Animations",
                        Type = "boolean",
                        DefaultValue = true
                    }
                }
            };

            // Generate UI elements
            var elements = uiGenerator.GenerateUIElements(schema);
            Console.WriteLine($"✓ Generated {elements.Count} UI elements");

            foreach (var element in elements)
            {
                Console.WriteLine($"  - {element.Label} ({element.Type})");
            }

            // Generate toggle UI
            var flag = await manager.GetFlagAsync("new-dashboard");
            if (flag != null)
            {
                var toggleUI = uiGenerator.GenerateToggleUI(flag);
                Console.WriteLine($"✓ Generated toggle UI: {toggleUI.Name}");
            }
        }
    }
}
