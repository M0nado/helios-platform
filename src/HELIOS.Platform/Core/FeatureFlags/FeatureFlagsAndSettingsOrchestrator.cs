using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HELIOS.Platform.Core.FeatureFlags.Models;
using HELIOS.Platform.Core.FeatureFlags.Persistence;
using HELIOS.Platform.Core.FeatureFlags.UI;

namespace HELIOS.Platform.Core.FeatureFlags
{
    /// <summary>
    /// Unified orchestrator for the entire feature flags and settings system
    /// </summary>
    public class FeatureFlagsAndSettingsOrchestrator
    {
        private readonly FeatureFlagManager _featureFlagManager;
        private readonly SettingsStore _settingsStore;
        private readonly ToggleableService _toggleableService;
        private readonly IFeatureFlagsPersistenceProvider _persistenceProvider;
        private readonly BackupRestoreManager _backupRestoreManager;
        private readonly ImportExportManager _importExportManager;
        private readonly SettingsUIGenerator _uiGenerator;
        private bool _isInitialized;

        public FeatureFlagsAndSettingsOrchestrator(string persistencePath)
        {
            _featureFlagManager = new FeatureFlagManager();
            _settingsStore = new SettingsStore();
            _toggleableService = new ToggleableService();
            _persistenceProvider = new JsonPersistenceProvider(persistencePath);
            _backupRestoreManager = new BackupRestoreManager(_persistenceProvider);
            _importExportManager = new ImportExportManager(_persistenceProvider);
            _uiGenerator = new SettingsUIGenerator();
            _isInitialized = false;
        }

        /// <summary>
        /// Initialize the system and load persisted data
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            try
            {
                // Load feature flags
                var flags = await _persistenceProvider.LoadFeatureFlagsAsync();
                foreach (var flag in flags)
                {
                    await _featureFlagManager.CreateFlagAsync(flag);
                }

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing system: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the feature flag manager
        /// </summary>
        public FeatureFlagManager GetFeatureFlagManager() => _featureFlagManager;

        /// <summary>
        /// Get the settings store
        /// </summary>
        public SettingsStore GetSettingsStore() => _settingsStore;

        /// <summary>
        /// Get the toggleable service
        /// </summary>
        public ToggleableService GetToggleableService() => _toggleableService;

        /// <summary>
        /// Get the backup/restore manager
        /// </summary>
        public BackupRestoreManager GetBackupRestoreManager() => _backupRestoreManager;

        /// <summary>
        /// Get the import/export manager
        /// </summary>
        public ImportExportManager GetImportExportManager() => _importExportManager;

        /// <summary>
        /// Get the UI generator
        /// </summary>
        public SettingsUIGenerator GetUIGenerator() => _uiGenerator;

        /// <summary>
        /// Persist all data
        /// </summary>
        public async Task PersistAsync()
        {
            try
            {
                var flags = await _featureFlagManager.GetAllFlagsAsync();
                await _persistenceProvider.SaveFeatureFlagsAsync(flags);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error persisting data: {ex.Message}");
            }
        }

        /// <summary>
        /// Evaluate a flag
        /// </summary>
        public async Task<bool> IsFlagEnabledAsync(string flagId, FeatureFlagContext context = null)
        {
            var result = await _featureFlagManager.EvaluateAsync(flagId, context);
            return result.Enabled;
        }

        /// <summary>
        /// Quick toggle check
        /// </summary>
        public async Task<bool> IsToggleEnabledAsync(string toggleId, string userId = null)
        {
            return await _toggleableService.IsEnabledAsync(toggleId, new FeatureFlagContext { UserId = userId });
        }

        /// <summary>
        /// Get all registered categories
        /// </summary>
        public List<ToggleableCategory> GetToggleableCategories()
        {
            return _toggleableService.GetCategories();
        }

        /// <summary>
        /// Shutdown and cleanup
        /// </summary>
        public async Task ShutdownAsync()
        {
            await PersistAsync();
            _featureFlagManager.ClearCache();
        }
    }

    /// <summary>
    /// Configuration for the orchestrator
    /// </summary>
    public class OrchestratorConfiguration
    {
        public string PersistencePath { get; set; }
        public bool AutoPersist { get; set; } = true;
        public int AutoPersistIntervalSeconds { get; set; } = 300;
        public bool EnableCaching { get; set; } = true;
        public int CacheTTLSeconds { get; set; } = 300;
        public bool EnableAnalytics { get; set; } = true;
        public int MaxAnalyticsEventCount { get; set; } = 10000;
    }
}
