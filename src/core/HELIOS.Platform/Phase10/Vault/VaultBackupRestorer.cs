using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace HELIOS.Platform.Phase10.Vault
{
    /// <summary>
    /// Manages automated backups, incremental backups, verification, and disaster recovery.
    /// </summary>
    public class VaultBackupRestorer
    {
        private readonly string _vaultPath;
        private readonly string _backupPath;
        private readonly IVaultEncryptionManager _encryptionManager;
        private readonly IVaultLogger _logger;
        private readonly string _backupMetadataPath;

        public VaultBackupRestorer(
            string vaultPath,
            string backupPath,
            IVaultEncryptionManager encryptionManager,
            IVaultLogger logger)
        {
            _vaultPath = vaultPath ?? throw new ArgumentNullException(nameof(vaultPath));
            _backupPath = backupPath ?? throw new ArgumentNullException(nameof(backupPath));
            _encryptionManager = encryptionManager ?? throw new ArgumentNullException(nameof(encryptionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _backupMetadataPath = Path.Combine(_backupPath, ".backup-meta.json");

            if (!Directory.Exists(_backupPath))
            {
                Directory.CreateDirectory(_backupPath);
            }
        }

        /// <summary>
        /// Creates a full backup of entire vault.
        /// </summary>
        public async Task<BackupResult> CreateFullBackupAsync(byte[] encryptionKey = null)
        {
            try
            {
                _logger.Log("Starting full vault backup...");

                var backupId = GenerateBackupId();
                var backupDir = Path.Combine(_backupPath, backupId);
                Directory.CreateDirectory(backupDir);

                var backupInfo = new BackupInfo
                {
                    BackupId = backupId,
                    BackupType = BackupType.Full,
                    CreatedAt = DateTime.UtcNow,
                    IsIncremental = false,
                    FileCount = 0,
                    TotalSize = 0,
                    IsEncrypted = encryptionKey != null,
                    IsVerified = false
                };

                // Copy all vault files
                long totalSize = 0;
                int fileCount = 0;

                var files = Directory.GetFiles(_vaultPath, "*", SearchOption.AllDirectories)
                    .Where(f => !f.Contains(".vault"))
                    .ToList();

                foreach (var file in files)
                {
                    var relativePath = Path.GetRelativePath(_vaultPath, file);
                    var destFile = Path.Combine(backupDir, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(destFile));

                    if (encryptionKey != null)
                    {
                        await _encryptionManager.EncryptFileAsync(file, encryptionKey);
                    }
                    else
                    {
                        File.Copy(file, destFile, overwrite: true);
                    }

                    totalSize += new FileInfo(file).Length;
                    fileCount++;
                }

                backupInfo.FileCount = fileCount;
                backupInfo.TotalSize = totalSize;

                // Create backup manifest
                await SaveBackupManifestAsync(backupDir, backupInfo);

                _logger.Log($"Full backup created: {backupId} ({totalSize} bytes, {fileCount} files)");

                return BackupResult.Success(backupId, totalSize, fileCount);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Full backup failed: {ex.Message}", ex);
                return BackupResult.Failure(ex.Message);
            }
        }

        /// <summary>
        /// Creates an incremental backup (only changed files).
        /// </summary>
        public async Task<BackupResult> CreateIncrementalBackupAsync(string baseBackupId, byte[] encryptionKey = null)
        {
            try
            {
                _logger.Log("Starting incremental vault backup...");

                var baseBackupDir = Path.Combine(_backupPath, baseBackupId);
                if (!Directory.Exists(baseBackupDir))
                {
                    return BackupResult.Failure("Base backup not found");
                }

                var backupId = GenerateBackupId();
                var backupDir = Path.Combine(_backupPath, backupId);
                Directory.CreateDirectory(backupDir);

                // Get base backup files for comparison
                var baseFiles = Directory.GetFiles(baseBackupDir, "*", SearchOption.AllDirectories)
                    .ToDictionary(f => Path.GetRelativePath(baseBackupDir, f));

                var backupInfo = new BackupInfo
                {
                    BackupId = backupId,
                    BackupType = BackupType.Incremental,
                    CreatedAt = DateTime.UtcNow,
                    IsIncremental = true,
                    BaseBackupId = baseBackupId,
                    FileCount = 0,
                    TotalSize = 0,
                    IsEncrypted = encryptionKey != null,
                    IsVerified = false
                };

                long totalSize = 0;
                int fileCount = 0;

                var currentFiles = Directory.GetFiles(_vaultPath, "*", SearchOption.AllDirectories)
                    .Where(f => !f.Contains(".vault"))
                    .ToList();

                foreach (var file in currentFiles)
                {
                    var relativePath = Path.GetRelativePath(_vaultPath, file);
                    var fileInfo = new FileInfo(file);
                    var lastModified = fileInfo.LastWriteTimeUtc;

                    // Only include if file is new or modified
                    if (!baseFiles.ContainsKey(relativePath) || 
                        lastModified > Directory.GetCreationTimeUtc(baseBackupDir))
                    {
                        var destFile = Path.Combine(backupDir, relativePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(destFile));

                        if (encryptionKey != null)
                        {
                            await _encryptionManager.EncryptFileAsync(file, encryptionKey);
                        }
                        else
                        {
                            File.Copy(file, destFile, overwrite: true);
                        }

                        totalSize += fileInfo.Length;
                        fileCount++;
                    }
                }

                backupInfo.FileCount = fileCount;
                backupInfo.TotalSize = totalSize;

                // Create backup manifest
                await SaveBackupManifestAsync(backupDir, backupInfo);

                _logger.Log($"Incremental backup created: {backupId} ({totalSize} bytes, {fileCount} files)");

                return BackupResult.Success(backupId, totalSize, fileCount);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Incremental backup failed: {ex.Message}", ex);
                return BackupResult.Failure(ex.Message);
            }
        }

        /// <summary>
        /// Verifies integrity of a backup.
        /// </summary>
        public async Task<BackupVerificationResult> VerifyBackupAsync(string backupId)
        {
            try
            {
                _logger.Log($"Verifying backup {backupId}...");

                var backupDir = Path.Combine(_backupPath, backupId);
                var manifestPath = Path.Combine(backupDir, "manifest.json");

                if (!File.Exists(manifestPath))
                {
                    return BackupVerificationResult.Failure("Backup manifest not found");
                }

                var json = await File.ReadAllTextAsync(manifestPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var expectedFileCount = root.GetProperty("fileCount").GetInt32();
                var expectedSize = root.GetProperty("totalSize").GetInt64();

                // Count actual files
                var actualFiles = Directory.GetFiles(backupDir, "*", SearchOption.AllDirectories)
                    .Where(f => !f.EndsWith("manifest.json"))
                    .ToList();

                long actualSize = 0;
                foreach (var file in actualFiles)
                {
                    actualSize += new FileInfo(file).Length;
                }

                var isValid = actualFiles.Count == expectedFileCount && actualSize == expectedSize;

                if (isValid)
                {
                    _logger.Log($"Backup {backupId} verification passed");
                }
                else
                {
                    _logger.LogError($"Backup {backupId} verification failed", new InvalidOperationException());
                }

                return new BackupVerificationResult
                {
                    IsValid = isValid,
                    BackupId = backupId,
                    ExpectedFileCount = expectedFileCount,
                    ActualFileCount = actualFiles.Count,
                    ExpectedSize = expectedSize,
                    ActualSize = actualSize,
                    Message = isValid ? "Verification passed" : "Verification failed"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Backup verification failed: {ex.Message}", ex);
                return BackupVerificationResult.Failure(ex.Message);
            }
        }

        /// <summary>
        /// Restores vault from a backup.
        /// </summary>
        public async Task<RestoreResult> RestoreFromBackupAsync(string backupId, bool preserveExisting = true)
        {
            try
            {
                _logger.Log($"Restoring vault from backup {backupId}...");

                var backupDir = Path.Combine(_backupPath, backupId);
                if (!Directory.Exists(backupDir))
                {
                    return RestoreResult.Failure("Backup not found");
                }

                // Verify backup first
                var verification = await VerifyBackupAsync(backupId);
                if (!verification.IsValid)
                {
                    return RestoreResult.Failure("Backup verification failed");
                }

                // Create recovery backup if preserving
                if (preserveExisting && Directory.GetFiles(_vaultPath, "*", SearchOption.AllDirectories).Length > 0)
                {
                    var recoveryBackupId = $"recovery_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
                    await CreateFullBackupAsync();
                    _logger.Log("Recovery backup created before restore");
                }

                // Restore files
                int filesRestored = 0;
                long sizeRestored = 0;

                var backupFiles = Directory.GetFiles(backupDir, "*", SearchOption.AllDirectories)
                    .Where(f => !f.EndsWith("manifest.json"))
                    .ToList();

                foreach (var file in backupFiles)
                {
                    var relativePath = Path.GetRelativePath(backupDir, file);
                    var destFile = Path.Combine(_vaultPath, relativePath);

                    Directory.CreateDirectory(Path.GetDirectoryName(destFile));
                    File.Copy(file, destFile, overwrite: true);

                    filesRestored++;
                    sizeRestored += new FileInfo(file).Length;
                }

                _logger.Log($"Vault restored from {backupId} ({filesRestored} files, {sizeRestored} bytes)");

                return RestoreResult.Success(backupId, filesRestored, sizeRestored);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Restore failed: {ex.Message}", ex);
                return RestoreResult.Failure(ex.Message);
            }
        }

        /// <summary>
        /// Gets list of available backups.
        /// </summary>
        public async Task<List<BackupInfo>> GetAvailableBackupsAsync()
        {
            try
            {
                var backups = new List<BackupInfo>();
                var backupDirs = Directory.GetDirectories(_backupPath).OrderByDescending(d => d).ToList();

                foreach (var dir in backupDirs)
                {
                    var manifestPath = Path.Combine(dir, "manifest.json");
                    if (File.Exists(manifestPath))
                    {
                        var json = await File.ReadAllTextAsync(manifestPath);
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        var backup = new BackupInfo
                        {
                            BackupId = root.GetProperty("backupId").GetString(),
                            CreatedAt = root.GetProperty("createdAt").GetDateTime(),
                            FileCount = root.GetProperty("fileCount").GetInt32(),
                            TotalSize = root.GetProperty("totalSize").GetInt64(),
                            IsEncrypted = root.GetProperty("isEncrypted").GetBoolean(),
                            IsVerified = root.GetProperty("isVerified").GetBoolean(),
                            IsIncremental = root.GetProperty("isIncremental").GetBoolean()
                        };

                        backups.Add(backup);
                    }
                }

                return backups;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to get available backups: {ex.Message}", ex);
                return new List<BackupInfo>();
            }
        }

        /// <summary>
        /// Schedules automatic backups.
        /// </summary>
        public async Task<bool> ScheduleAutomaticBackupAsync(int intervalHours = 24, bool incremental = false)
        {
            try
            {
                var schedule = new
                {
                    enabled = true,
                    intervalHours,
                    incremental,
                    lastRun = DateTime.MinValue,
                    nextRun = DateTime.UtcNow.AddHours(intervalHours)
                };

                var schedulePath = Path.Combine(_backupPath, "schedule.json");
                var json = JsonSerializer.Serialize(schedule, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(schedulePath, json);

                _logger.Log($"Automatic backup scheduled every {intervalHours} hours (incremental: {incremental})");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to schedule backup: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Removes old backups based on retention policy.
        /// </summary>
        public async Task<int> CleanupOldBackupsAsync(int retentionDays = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
                var backupDirs = Directory.GetDirectories(_backupPath).ToList();
                int removedCount = 0;

                foreach (var dir in backupDirs)
                {
                    var creationTime = Directory.GetCreationTimeUtc(dir);
                    if (creationTime < cutoffDate)
                    {
                        Directory.Delete(dir, recursive: true);
                        removedCount++;
                        _logger.Log($"Removed old backup: {Path.GetFileName(dir)}");
                    }
                }

                _logger.Log($"Cleanup complete: {removedCount} backups removed");
                return await Task.FromResult(removedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Cleanup failed: {ex.Message}", ex);
                return 0;
            }
        }

        private async Task SaveBackupManifestAsync(string backupDir, BackupInfo backupInfo)
        {
            var manifest = new
            {
                backupInfo.BackupId,
                backupInfo.BackupType,
                backupInfo.CreatedAt,
                backupInfo.IsIncremental,
                backupInfo.BaseBackupId,
                backupInfo.FileCount,
                backupInfo.TotalSize,
                backupInfo.IsEncrypted,
                isVerified = false
            };

            var manifestPath = Path.Combine(backupDir, "manifest.json");
            var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(manifestPath, json);
        }

        private string GenerateBackupId()
        {
            return $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString().Substring(0, 8)}";
        }
    }

    public class BackupInfo
    {
        public string BackupId { get; set; }
        public BackupType BackupType { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsIncremental { get; set; }
        public string BaseBackupId { get; set; }
        public int FileCount { get; set; }
        public long TotalSize { get; set; }
        public bool IsEncrypted { get; set; }
        public bool IsVerified { get; set; }
    }

    public enum BackupType
    {
        Full = 0,
        Incremental = 1,
        Differential = 2
    }

    public class BackupResult
    {
        public bool IsSuccess { get; set; }
        public string BackupId { get; set; }
        public long TotalSize { get; set; }
        public int FileCount { get; set; }
        public string Message { get; set; }

        public static BackupResult Success(string backupId, long size, int fileCount) => new()
        {
            IsSuccess = true,
            BackupId = backupId,
            TotalSize = size,
            FileCount = fileCount,
            Message = "Backup created successfully"
        };

        public static BackupResult Failure(string message) => new()
        {
            IsSuccess = false,
            Message = message
        };
    }

    public class BackupVerificationResult
    {
        public bool IsValid { get; set; }
        public string BackupId { get; set; }
        public int ExpectedFileCount { get; set; }
        public int ActualFileCount { get; set; }
        public long ExpectedSize { get; set; }
        public long ActualSize { get; set; }
        public string Message { get; set; }

        public static BackupVerificationResult Failure(string message) => new()
        {
            IsValid = false,
            Message = message
        };
    }

    public class RestoreResult
    {
        public bool IsSuccess { get; set; }
        public string BackupId { get; set; }
        public int FilesRestored { get; set; }
        public long SizeRestored { get; set; }
        public string Message { get; set; }

        public static RestoreResult Success(string backupId, int fileCount, long size) => new()
        {
            IsSuccess = true,
            BackupId = backupId,
            FilesRestored = fileCount,
            SizeRestored = size,
            Message = "Restore completed successfully"
        };

        public static RestoreResult Failure(string message) => new()
        {
            IsSuccess = false,
            Message = message
        };
    }
}
