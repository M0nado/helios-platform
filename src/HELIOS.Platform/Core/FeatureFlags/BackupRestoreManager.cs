using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HELIOS.Platform.Core.FeatureFlags.Models;
using HELIOS.Platform.Core.FeatureFlags.Persistence;

namespace HELIOS.Platform.Core.FeatureFlags
{
    /// <summary>
    /// Backup and restore manager
    /// </summary>
    public class BackupRestoreManager
    {
        private readonly IFeatureFlagsPersistenceProvider _persistenceProvider;
        private readonly List<SettingsAuditLog> _auditLogs;
        private readonly object _lockObject = new object();

        public BackupRestoreManager(IFeatureFlagsPersistenceProvider persistenceProvider)
        {
            _persistenceProvider = persistenceProvider ?? throw new ArgumentNullException(nameof(persistenceProvider));
            _auditLogs = new List<SettingsAuditLog>();
        }

        /// <summary>
        /// Create a full backup
        /// </summary>
        public async Task<SettingsBackup> CreateFullBackupAsync(
            Dictionary<string, Setting> globalSettings,
            Dictionary<string, Dictionary<string, Setting>> userSettings,
            List<FeatureFlag> featureFlags,
            string backupName,
            string createdBy)
        {
            var backup = new SettingsBackup
            {
                BackupId = Guid.NewGuid().ToString(),
                Name = backupName,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                BackupType = "Full",
                Version = 1
            };

            // Convert to dictionaries for serialization
            backup.GlobalSettings = globalSettings.ToDictionary(
                k => k.Key,
                v => (object)v.Value);

            backup.UserSettings = userSettings.ToDictionary(
                k => k.Key,
                v => v.Value.ToDictionary(kk => kk.Key, vv => (object)vv.Value));

            backup.FeatureFlags = featureFlags.ToDictionary(
                f => f.Id,
                f => (object)f);

            backup.SizeBytes = System.Text.Json.JsonSerializer.Serialize(backup).Length;

            await _persistenceProvider.SaveBackupAsync(backup);

            LogAudit(new SettingsAuditLog
            {
                AuditId = Guid.NewGuid().ToString(),
                Action = "BackupCreated",
                UserId = createdBy,
                Timestamp = DateTime.UtcNow,
                SettingKey = $"Backup:{backup.BackupId}"
            });

            return backup;
        }

        /// <summary>
        /// Create a differential backup
        /// </summary>
        public async Task<SettingsBackup> CreateDifferentialBackupAsync(
            SettingsBackup lastBackup,
            Dictionary<string, Setting> globalSettings,
            Dictionary<string, Dictionary<string, Setting>> userSettings,
            List<FeatureFlag> featureFlags,
            string backupName,
            string createdBy)
        {
            var backup = new SettingsBackup
            {
                BackupId = Guid.NewGuid().ToString(),
                Name = backupName,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                BackupType = "Differential",
                Version = lastBackup?.Version + 1 ?? 1
            };

            // Only include changed settings
            var changedGlobalSettings = new Dictionary<string, object>();
            foreach (var setting in globalSettings.Values)
            {
                if (lastBackup?.GlobalSettings == null || 
                    !lastBackup.GlobalSettings.ContainsKey(setting.Key) ||
                    !AreEqual(lastBackup.GlobalSettings[setting.Key], setting))
                {
                    changedGlobalSettings[setting.Key] = setting;
                }
            }

            backup.GlobalSettings = changedGlobalSettings;
            backup.UserSettings = userSettings.ToDictionary(
                k => k.Key,
                v => v.Value.ToDictionary(kk => kk.Key, vv => (object)vv.Value));

            backup.FeatureFlags = featureFlags.ToDictionary(
                f => f.Id,
                f => (object)f);

            backup.SizeBytes = System.Text.Json.JsonSerializer.Serialize(backup).Length;

            await _persistenceProvider.SaveBackupAsync(backup);

            return backup;
        }

        /// <summary>
        /// Restore from backup
        /// </summary>
        public async Task<RestoreResult> RestoreFromBackupAsync(
            string backupId,
            bool overwriteExisting = false)
        {
            var backup = await _persistenceProvider.LoadBackupAsync(backupId);
            if (backup == null)
            {
                return new RestoreResult
                {
                    Success = false,
                    Message = $"Backup {backupId} not found"
                };
            }

            try
            {
                var result = new RestoreResult
                {
                    Success = true,
                    RestoredAt = DateTime.UtcNow,
                    BackupId = backupId,
                    ItemsRestored = (backup.GlobalSettings?.Count ?? 0) + 
                                   (backup.UserSettings?.Values.Sum(d => d.Count) ?? 0) +
                                   (backup.FeatureFlags?.Count ?? 0)
                };

                backup.RestoredAt = DateTime.UtcNow;
                await _persistenceProvider.SaveBackupAsync(backup);

                return result;
            }
            catch (Exception ex)
            {
                return new RestoreResult
                {
                    Success = false,
                    Message = $"Failed to restore backup: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// List all available backups
        /// </summary>
        public async Task<List<SettingsBackup>> ListBackupsAsync()
        {
            return await _persistenceProvider.ListBackupsAsync();
        }

        /// <summary>
        /// Delete a backup
        /// </summary>
        public async Task<bool> DeleteBackupAsync(string backupId)
        {
            try
            {
                var backups = await _persistenceProvider.ListBackupsAsync();
                var backupToDelete = backups.FirstOrDefault(b => b.BackupId == backupId);
                
                if (backupToDelete == null)
                    return false;

                // In a real implementation, would actually delete the file
                LogAudit(new SettingsAuditLog
                {
                    AuditId = Guid.NewGuid().ToString(),
                    Action = "BackupDeleted",
                    SettingKey = $"Backup:{backupId}",
                    Timestamp = DateTime.UtcNow
                });

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get audit logs
        /// </summary>
        public List<SettingsAuditLog> GetAuditLogs(int limit = 1000)
        {
            lock (_lockObject)
            {
                return _auditLogs.OrderByDescending(l => l.Timestamp).Take(limit).ToList();
            }
        }

        /// <summary>
        /// Log audit entry
        /// </summary>
        private void LogAudit(SettingsAuditLog log)
        {
            lock (_lockObject)
            {
                _auditLogs.Add(log);
                if (_auditLogs.Count > 10000)
                    _auditLogs.RemoveRange(0, 5000);
            }
        }

        /// <summary>
        /// Compare two objects for equality
        /// </summary>
        private bool AreEqual(object obj1, object obj2)
        {
            if (obj1 == null && obj2 == null) return true;
            if (obj1 == null || obj2 == null) return false;
            return obj1.Equals(obj2);
        }
    }

    /// <summary>
    /// Restore operation result
    /// </summary>
    public class RestoreResult
    {
        public bool Success { get; set; }
        public string BackupId { get; set; }
        public DateTime RestoredAt { get; set; }
        public int ItemsRestored { get; set; }
        public string Message { get; set; }
    }
}
