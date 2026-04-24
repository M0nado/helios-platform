using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace HELIOS.Platform.Phase10.Vault
{
    /// <summary>
    /// Manages locker operations including creation, configuration, backup, and restoration.
    /// </summary>
    public class VaultLockerManager
    {
        private readonly string _vaultPath;
        private readonly string _backupPath;
        private readonly IVaultLogger _logger;
        private readonly IVaultEncryptionManager _encryptionManager;

        public VaultLockerManager(
            string vaultPath,
            string backupPath,
            IVaultEncryptionManager encryptionManager,
            IVaultLogger logger)
        {
            _vaultPath = vaultPath ?? throw new ArgumentNullException(nameof(vaultPath));
            _backupPath = backupPath ?? throw new ArgumentNullException(nameof(backupPath));
            _encryptionManager = encryptionManager ?? throw new ArgumentNullException(nameof(encryptionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new custom locker.
        /// </summary>
        public async Task<bool> CreateLockerAsync(string lockerName, long maxSizeBytes = 0)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(lockerName))
                {
                    _logger.LogError("Invalid locker name", new ArgumentException("Name cannot be empty"));
                    return false;
                }

                var lockerPath = Path.Combine(_vaultPath, lockerName);

                if (Directory.Exists(lockerPath))
                {
                    _logger.LogError($"Locker {lockerName} already exists", new InvalidOperationException());
                    return false;
                }

                Directory.CreateDirectory(lockerPath);

                // Create locker metadata
                var metadata = new
                {
                    name = lockerName,
                    created = DateTime.UtcNow,
                    maxSize = maxSizeBytes,
                    currentSize = 0L,
                    itemCount = 0,
                    encrypted = true,
                    backupEnabled = true,
                    lastBackup = DateTime.MinValue
                };

                var metadataPath = Path.Combine(lockerPath, ".locker-meta.json");
                var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(metadataPath, json);

                _logger.Log($"Locker '{lockerName}' created successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to create locker: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Renames an existing locker.
        /// </summary>
        public async Task<bool> RenameLockerAsync(string oldName, string newName)
        {
            try
            {
                var oldPath = Path.Combine(_vaultPath, oldName);
                var newPath = Path.Combine(_vaultPath, newName);

                if (!Directory.Exists(oldPath))
                {
                    return false;
                }

                if (Directory.Exists(newPath))
                {
                    return false;
                }

                Directory.Move(oldPath, newPath);

                // Update metadata
                var metadataPath = Path.Combine(newPath, ".locker-meta.json");
                if (File.Exists(metadataPath))
                {
                    var json = await File.ReadAllTextAsync(metadataPath);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    var updated = new
                    {
                        name = newName,
                        created = root.GetProperty("created").GetDateTime(),
                        maxSize = root.GetProperty("maxSize").GetInt64(),
                        currentSize = root.GetProperty("currentSize").GetInt64(),
                        itemCount = root.GetProperty("itemCount").GetInt32(),
                        encrypted = root.GetProperty("encrypted").GetBoolean(),
                        backupEnabled = root.GetProperty("backupEnabled").GetBoolean(),
                        lastBackup = root.GetProperty("lastBackup").GetDateTime(),
                        renamedAt = DateTime.UtcNow
                    };

                    var updatedJson = JsonSerializer.Serialize(updated, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(metadataPath, updatedJson);
                }

                _logger.Log($"Locker renamed: {oldName} -> {newName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to rename locker: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Configures size limits for a locker.
        /// </summary>
        public async Task<bool> SetSizeLimitAsync(string lockerName, long maxSizeBytes)
        {
            try
            {
                var lockerPath = Path.Combine(_vaultPath, lockerName);
                var metadataPath = Path.Combine(lockerPath, ".locker-meta.json");

                if (!File.Exists(metadataPath))
                {
                    return false;
                }

                var json = await File.ReadAllTextAsync(metadataPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var updated = new
                {
                    name = root.GetProperty("name").GetString(),
                    created = root.GetProperty("created").GetDateTime(),
                    maxSize = maxSizeBytes,
                    currentSize = root.GetProperty("currentSize").GetInt64(),
                    itemCount = root.GetProperty("itemCount").GetInt32(),
                    encrypted = root.GetProperty("encrypted").GetBoolean(),
                    backupEnabled = root.GetProperty("backupEnabled").GetBoolean(),
                    lastBackup = root.GetProperty("lastBackup").GetDateTime()
                };

                var updatedJson = JsonSerializer.Serialize(updated, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(metadataPath, updatedJson);

                _logger.Log($"Size limit set for {lockerName}: {maxSizeBytes} bytes");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to set size limit: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Gets current size of locker.
        /// </summary>
        public async Task<long> GetLockerSizeAsync(string lockerName)
        {
            try
            {
                var lockerPath = Path.Combine(_vaultPath, lockerName);

                if (!Directory.Exists(lockerPath))
                {
                    return 0;
                }

                long size = 0;
                var files = Directory.GetFiles(lockerPath, "*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    size += new FileInfo(file).Length;
                }

                return await Task.FromResult(size);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to get locker size: {ex.Message}", ex);
                return 0;
            }
        }

        /// <summary>
        /// Backs up a specific locker to backup partition.
        /// </summary>
        public async Task<bool> BackupLockerAsync(string lockerName)
        {
            try
            {
                var lockerPath = Path.Combine(_vaultPath, lockerName);

                if (!Directory.Exists(lockerPath))
                {
                    return false;
                }

                var backupDir = Path.Combine(_backupPath, lockerName, DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));
                Directory.CreateDirectory(backupDir);

                // Copy all files
                var files = Directory.GetFiles(lockerPath, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var relativePath = Path.GetRelativePath(lockerPath, file);
                    var destFile = Path.Combine(backupDir, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(destFile));
                    File.Copy(file, destFile, overwrite: true);
                }

                // Update metadata
                var metadataPath = Path.Combine(lockerPath, ".locker-meta.json");
                if (File.Exists(metadataPath))
                {
                    var json = await File.ReadAllTextAsync(metadataPath);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    var updated = new
                    {
                        name = root.GetProperty("name").GetString(),
                        created = root.GetProperty("created").GetDateTime(),
                        maxSize = root.GetProperty("maxSize").GetInt64(),
                        currentSize = root.GetProperty("currentSize").GetInt64(),
                        itemCount = root.GetProperty("itemCount").GetInt32(),
                        encrypted = root.GetProperty("encrypted").GetBoolean(),
                        backupEnabled = root.GetProperty("backupEnabled").GetBoolean(),
                        lastBackup = DateTime.UtcNow
                    };

                    var updatedJson = JsonSerializer.Serialize(updated, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(metadataPath, updatedJson);
                }

                _logger.Log($"Locker '{lockerName}' backed up to {backupDir}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to backup locker: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Restores a locker from backup.
        /// </summary>
        public async Task<bool> RestoreLockerAsync(string lockerName, string backupTimestamp = null)
        {
            try
            {
                var backupDir = Path.Combine(_backupPath, lockerName);

                if (!Directory.Exists(backupDir))
                {
                    return false;
                }

                // Find latest backup or specified one
                var backups = Directory.GetDirectories(backupDir).OrderByDescending(d => d).ToList();

                if (backups.Count == 0)
                {
                    return false;
                }

                var restoreFrom = backupTimestamp != null
                    ? backups.FirstOrDefault(b => b.EndsWith(backupTimestamp))
                    : backups.First();

                if (restoreFrom == null)
                {
                    return false;
                }

                var targetPath = Path.Combine(_vaultPath, lockerName);

                // Remove existing locker (backup first)
                if (Directory.Exists(targetPath))
                {
                    var tempBackup = Path.Combine(_backupPath, $"{lockerName}_pre_restore_{DateTime.UtcNow:yyyyMMdd_HHmmss}");
                    Directory.CreateDirectory(Path.GetDirectoryName(tempBackup));
                    Directory.Move(targetPath, tempBackup);
                }

                // Restore from backup
                Directory.CreateDirectory(targetPath);
                var files = Directory.GetFiles(restoreFrom, "*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    var relativePath = Path.GetRelativePath(restoreFrom, file);
                    var destFile = Path.Combine(targetPath, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(destFile));
                    File.Copy(file, destFile, overwrite: true);
                }

                _logger.Log($"Locker '{lockerName}' restored from {restoreFrom}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to restore locker: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Gets list of all lockers.
        /// </summary>
        public async Task<List<LockerInfo>> GetAllLockersAsync()
        {
            try
            {
                var lockers = new List<LockerInfo>();
                var directories = Directory.GetDirectories(_vaultPath)
                    .Where(d => !Path.GetFileName(d).StartsWith("."))
                    .ToList();

                foreach (var dir in directories)
                {
                    var lockerName = Path.GetFileName(dir);
                    var metadataPath = Path.Combine(dir, ".locker-meta.json");

                    if (File.Exists(metadataPath))
                    {
                        var json = await File.ReadAllTextAsync(metadataPath);
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        var size = await GetLockerSizeAsync(lockerName);

                        lockers.Add(new LockerInfo
                        {
                            Name = lockerName,
                            Created = root.GetProperty("created").GetDateTime(),
                            MaxSize = root.GetProperty("maxSize").GetInt64(),
                            CurrentSize = size,
                            ItemCount = root.GetProperty("itemCount").GetInt32(),
                            Encrypted = root.GetProperty("encrypted").GetBoolean(),
                            LastBackup = root.GetProperty("lastBackup").GetDateTime()
                        });
                    }
                }

                return lockers;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to get lockers: {ex.Message}", ex);
                return new List<LockerInfo>();
            }
        }

        /// <summary>
        /// Performs maintenance on locker (cleanup, defrag).
        /// </summary>
        public async Task<MaintenanceResult> PerformMaintenanceAsync(string lockerName)
        {
            try
            {
                var lockerPath = Path.Combine(_vaultPath, lockerName);

                if (!Directory.Exists(lockerPath))
                {
                    return MaintenanceResult.Failure("Locker not found");
                }

                var result = new MaintenanceResult { IsSuccess = true };

                // Remove orphaned files
                var tempFiles = Directory.GetFiles(lockerPath, "*.tmp", SearchOption.AllDirectories);
                foreach (var file in tempFiles)
                {
                    File.Delete(file);
                    result.FilesRemoved++;
                }

                result.Message = $"Maintenance complete: {result.FilesRemoved} files removed";
                _logger.Log($"Maintenance performed on '{lockerName}': {result.Message}");

                return await Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Maintenance failed: {ex.Message}", ex);
                return MaintenanceResult.Failure(ex.Message);
            }
        }

        /// <summary>
        /// Syncs locker with backup partition.
        /// </summary>
        public async Task<bool> SyncWithBackupAsync(string lockerName)
        {
            try
            {
                var lockerPath = Path.Combine(_vaultPath, lockerName);

                if (!Directory.Exists(lockerPath))
                {
                    return false;
                }

                // Backup locker
                if (!await BackupLockerAsync(lockerName))
                {
                    return false;
                }

                _logger.Log($"Locker '{lockerName}' synced with backup");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Sync with backup failed: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Deletes a locker permanently.
        /// </summary>
        public async Task<bool> DeleteLockerAsync(string lockerName, bool createBackupFirst = true)
        {
            try
            {
                var lockerPath = Path.Combine(_vaultPath, lockerName);

                if (!Directory.Exists(lockerPath))
                {
                    return false;
                }

                // Create backup first if requested
                if (createBackupFirst)
                {
                    await BackupLockerAsync(lockerName);
                }

                Directory.Delete(lockerPath, recursive: true);
                _logger.Log($"Locker '{lockerName}' deleted");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to delete locker: {ex.Message}", ex);
                return false;
            }
        }
    }

    public class LockerInfo
    {
        public string Name { get; set; }
        public DateTime Created { get; set; }
        public long MaxSize { get; set; }
        public long CurrentSize { get; set; }
        public int ItemCount { get; set; }
        public bool Encrypted { get; set; }
        public DateTime LastBackup { get; set; }
    }

    public class MaintenanceResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public int FilesRemoved { get; set; }

        public static MaintenanceResult Failure(string message) => new() { IsSuccess = false, Message = message };
    }
}
