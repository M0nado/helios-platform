using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;

namespace HELIOS.Platform.Phase10.Vault
{
    /// <summary>
    /// Integration bridge for vault with external systems (KeePass, file system, UI).
    /// </summary>
    public class VaultIntegrationBridge
    {
        private readonly string _vaultPath;
        private readonly IVaultLockerManager _lockerManager;
        private readonly IVaultEncryptionManager _encryptionManager;
        private readonly IVaultLogger _logger;

        public VaultIntegrationBridge(
            string vaultPath,
            IVaultLockerManager lockerManager,
            IVaultEncryptionManager encryptionManager,
            IVaultLogger logger)
        {
            _vaultPath = vaultPath ?? throw new ArgumentNullException(nameof(vaultPath));
            _lockerManager = lockerManager ?? throw new ArgumentNullException(nameof(lockerManager));
            _encryptionManager = encryptionManager ?? throw new ArgumentNullException(nameof(encryptionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Integrates with KeePass password manager.
        /// </summary>
        public async Task<bool> IntegrateWithKeePassAsync(string keepassDbPath, string masterPassword)
        {
            try
            {
                _logger.Log("Integrating with KeePass...");

                if (!File.Exists(keepassDbPath))
                {
                    _logger.LogError("KeePass database not found", new FileNotFoundException(keepassDbPath));
                    return false;
                }

                var integrationConfig = new
                {
                    type = "KeePass",
                    dbPath = keepassDbPath,
                    integrated = DateTime.UtcNow,
                    status = "active",
                    vaultPath = _vaultPath
                };

                var configPath = Path.Combine(_vaultPath, ".vault", "keepass-config.json");
                var json = JsonSerializer.Serialize(integrationConfig, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(configPath, json);

                _logger.Log("KeePass integration successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"KeePass integration failed: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Adds file to vault using drag-and-drop.
        /// </summary>
        public async Task<bool> AddFileByDragDropAsync(string sourceFilePath, string targetLocker, string sessionId)
        {
            try
            {
                if (!File.Exists(sourceFilePath))
                {
                    return false;
                }

                var lockerPath = Path.Combine(_vaultPath, targetLocker);
                if (!Directory.Exists(lockerPath))
                {
                    return false;
                }

                var fileName = Path.GetFileName(sourceFilePath);
                var destPath = Path.Combine(lockerPath, fileName);

                // Create copy in vault
                File.Copy(sourceFilePath, destPath, overwrite: true);

                _logger.Log($"File added to vault via drag-drop: {fileName} -> {targetLocker}");
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Drag-drop add failed: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Adds context menu "Add to Vault" option support.
        /// </summary>
        public async Task<bool> RegisterContextMenuAsync()
        {
            try
            {
                _logger.Log("Registering context menu integration...");

                var contextMenuConfig = new
                {
                    action = "AddToVault",
                    displayName = "Add to Vault",
                    handler = "VaultIntegrationBridge.HandleContextMenu",
                    vaultPath = _vaultPath,
                    enabled = true
                };

                var configPath = Path.Combine(_vaultPath, ".vault", "context-menu-config.json");
                var json = JsonSerializer.Serialize(contextMenuConfig, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(configPath, json);

                _logger.Log("Context menu integration registered");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Context menu registration failed: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Handles context menu action.
        /// </summary>
        public async Task<bool> HandleContextMenuAddToVaultAsync(string filePath, string targetLocker)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return false;
                }

                var lockerPath = Path.Combine(_vaultPath, targetLocker);
                if (!Directory.Exists(lockerPath))
                {
                    return false;
                }

                var fileName = Path.GetFileName(filePath);
                var destPath = Path.Combine(lockerPath, fileName);

                File.Copy(filePath, destPath, overwrite: true);

                _logger.Log($"File added via context menu: {fileName} -> {targetLocker}");
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Context menu action failed: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Integrates with HELIOS UI for vault management.
        /// </summary>
        public async Task<VaultUIState> GetUIStateAsync()
        {
            try
            {
                var lockers = await _lockerManager.GetAllLockersAsync();

                var state = new VaultUIState
                {
                    IsInitialized = Directory.Exists(_vaultPath),
                    Lockers = lockers.Select(l => new LockerUIInfo
                    {
                        Name = l.Name,
                        Size = l.CurrentSize,
                        ItemCount = l.ItemCount,
                        IsEncrypted = l.Encrypted,
                        LastModified = l.Created
                    }).ToList(),
                    LastBackup = await GetLastBackupTimeAsync(),
                    SyncStatus = "In sync"
                };

                return state;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to get UI state: {ex.Message}", ex);
                return new VaultUIState { IsInitialized = false };
            }
        }

        /// <summary>
        /// Syncs vault with HELIOS backup system.
        /// </summary>
        public async Task<bool> SyncWithBackupSystemAsync(string backupPartitionPath)
        {
            try
            {
                _logger.Log("Syncing with backup system...");

                if (!Directory.Exists(backupPartitionPath))
                {
                    Directory.CreateDirectory(backupPartitionPath);
                }

                // Sync all lockers
                var syncResults = new List<bool>();
                var vaultDirs = Directory.GetDirectories(_vaultPath)
                    .Where(d => !Path.GetFileName(d).StartsWith("."))
                    .ToList();

                foreach (var dir in vaultDirs)
                {
                    var lockerName = Path.GetFileName(dir);
                    var result = await _lockerManager.SyncWithBackupAsync(lockerName);
                    syncResults.Add(result);
                }

                var successCount = syncResults.Count(r => r);
                _logger.Log($"Backup sync completed: {successCount}/{syncResults.Count} lockers synced");
                return successCount == syncResults.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Backup sync failed: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Integrates with quarantine system for threat isolation.
        /// </summary>
        public async Task<bool> IsolateToQuarantineAsync(string filePath, string reason)
        {
            try
            {
                var quarantineLocker = "Quarantine";
                var lockerPath = Path.Combine(_vaultPath, quarantineLocker);

                // Create quarantine locker if it doesn't exist
                if (!Directory.Exists(lockerPath))
                {
                    Directory.CreateDirectory(lockerPath);
                }

                var fileName = Path.GetFileName(filePath);
                var quarantineName = $"{Path.GetFileNameWithoutExtension(fileName)}_QUARANTINE_{DateTime.UtcNow:yyyyMMdd_HHmmss}{Path.GetExtension(filePath)}";
                var quarantinePath = Path.Combine(lockerPath, quarantineName);

                if (File.Exists(filePath))
                {
                    File.Copy(filePath, quarantinePath, overwrite: true);
                }

                // Log quarantine event
                var quarantineLog = new
                {
                    timestamp = DateTime.UtcNow,
                    originalPath = filePath,
                    quarantinePath,
                    reason,
                    filename = fileName
                };

                var logPath = Path.Combine(lockerPath, "quarantine.log");
                var json = JsonSerializer.Serialize(quarantineLog);
                await File.AppendAllTextAsync(logPath, json + Environment.NewLine);

                _logger.Log($"File isolated to quarantine: {fileName} (Reason: {reason})");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Quarantine isolation failed: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Configures scheduled backups through integration.
        /// </summary>
        public async Task<bool> ConfigureScheduledBackupsAsync(int intervalHours = 24, bool incremental = true)
        {
            try
            {
                _logger.Log($"Configuring scheduled backups: every {intervalHours} hours (incremental: {incremental})");

                var scheduleConfig = new
                {
                    enabled = true,
                    intervalHours,
                    incremental,
                    scheduler = "HELIOS.VaultBackupScheduler",
                    createdAt = DateTime.UtcNow
                };

                var schedulePath = Path.Combine(_vaultPath, ".vault", "backup-schedule.json");
                var json = JsonSerializer.Serialize(scheduleConfig, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(schedulePath, json);

                _logger.Log("Scheduled backups configured");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Schedule configuration failed: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Gets integration status with all systems.
        /// </summary>
        public async Task<IntegrationStatus> GetIntegrationStatusAsync()
        {
            try
            {
                var status = new IntegrationStatus
                {
                    LastChecked = DateTime.UtcNow,
                    Integrations = new Dictionary<string, bool>()
                };

                // Check KeePass integration
                var keepassConfigPath = Path.Combine(_vaultPath, ".vault", "keepass-config.json");
                status.Integrations["KeePass"] = File.Exists(keepassConfigPath);

                // Check context menu integration
                var contextMenuPath = Path.Combine(_vaultPath, ".vault", "context-menu-config.json");
                status.Integrations["ContextMenu"] = File.Exists(contextMenuPath);

                // Check backup schedule
                var schedulePath = Path.Combine(_vaultPath, ".vault", "backup-schedule.json");
                status.Integrations["BackupScheduler"] = File.Exists(schedulePath);

                // Check quarantine
                var quarantinePath = Path.Combine(_vaultPath, "Quarantine");
                status.Integrations["Quarantine"] = Directory.Exists(quarantinePath);

                return await Task.FromResult(status);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to get integration status: {ex.Message}", ex);
                return new IntegrationStatus { LastChecked = DateTime.UtcNow };
            }
        }

        /// <summary>
        /// Enables file system integration for direct access.
        /// </summary>
        public async Task<bool> EnableFileSystemIntegrationAsync()
        {
            try
            {
                _logger.Log("Enabling file system integration...");

                var fsConfig = new
                {
                    enabled = true,
                    vaultPath = _vaultPath,
                    autoMount = true,
                    allowDirectAccess = true,
                    enableShortcuts = true
                };

                var configPath = Path.Combine(_vaultPath, ".vault", "fs-integration.json");
                var json = JsonSerializer.Serialize(fsConfig, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(configPath, json);

                _logger.Log("File system integration enabled");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"File system integration failed: {ex.Message}", ex);
                return false;
            }
        }

        private async Task<DateTime?> GetLastBackupTimeAsync()
        {
            try
            {
                var backupPath = Path.Combine(_vaultPath, ".vault", "last-backup");
                if (File.Exists(backupPath))
                {
                    var content = await File.ReadAllTextAsync(backupPath);
                    if (DateTime.TryParse(content, out var backupTime))
                    {
                        return backupTime;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }

    public interface IVaultLockerManager
    {
        Task<List<LockerInfo>> GetAllLockersAsync();
        Task<bool> SyncWithBackupAsync(string lockerName);
    }

    public class VaultUIState
    {
        public bool IsInitialized { get; set; }
        public List<LockerUIInfo> Lockers { get; set; } = new();
        public DateTime? LastBackup { get; set; }
        public string SyncStatus { get; set; }
    }

    public class LockerUIInfo
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public int ItemCount { get; set; }
        public bool IsEncrypted { get; set; }
        public DateTime LastModified { get; set; }
    }

    public class IntegrationStatus
    {
        public DateTime LastChecked { get; set; }
        public Dictionary<string, bool> Integrations { get; set; } = new();
    }
}
