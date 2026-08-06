using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace HELIOS.Platform.Phase10.Vault
{
    /// <summary>
    /// Manages full backups, verification, and disaster recovery.
    /// Incremental backups remain disabled until their manifests can represent
    /// deletions and retention can preserve complete dependency chains.
    /// </summary>
    public class VaultBackupRestorer
    {
        private readonly string _vaultPath;
        private readonly string _backupPath;
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
            _ = encryptionManager ?? throw new ArgumentNullException(nameof(encryptionManager));
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

                if (encryptionKey != null)
                {
                    _logger.Log(EncryptedBackupsUnavailableMessage);
                    return BackupResult.Failure(EncryptedBackupsUnavailableMessage);
                }

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
                    IsEncrypted = false,
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

                    File.Copy(file, destFile, overwrite: true);
                    totalSize += new FileInfo(destFile).Length;
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
        /// Incremental backups are intentionally unavailable until deletion
        /// tombstones and dependency-aware retention are implemented.
        /// </summary>
        public Task<BackupResult> CreateIncrementalBackupAsync(string baseBackupId, byte[] encryptionKey = null)
        {
            _logger.Log(IncrementalBackupsUnavailableMessage);
            return Task.FromResult(BackupResult.Failure(IncrementalBackupsUnavailableMessage));
        }

        /// <summary>
        /// Verifies integrity of a backup.
        /// </summary>
        public async Task<BackupVerificationResult> VerifyBackupAsync(string backupId)
        {
            try
            {
                _logger.Log($"Verifying backup {backupId}...");

                var backup = await LoadValidatedBackupAsync(backupId);
                EnsureFullBackupIsSupported(backup.Manifest);
                if (backup.Manifest.IsEncrypted)
                {
                    throw new NotSupportedException(EncryptedBackupsUnavailableMessage);
                }
                var expectedFileCount = backup.Manifest.FileCount;
                var expectedSize = backup.Manifest.TotalSize;
                var actualFileCount = backup.Files.Count;
                var actualSize = backup.Files.Sum(file => file.Size);

                _logger.Log($"Backup {backupId} verification passed");

                return new BackupVerificationResult
                {
                    IsValid = true,
                    BackupId = backupId,
                    ExpectedFileCount = expectedFileCount,
                    ActualFileCount = actualFileCount,
                    ExpectedSize = expectedSize,
                    ActualSize = actualSize,
                    Message = "Verification passed"
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
        public Task<RestoreResult> RestoreFromBackupAsync(string backupId, bool preserveExisting = true)
        {
            _logger.Log(RestoreUnavailableMessage);
            return Task.FromResult(RestoreResult.Failure(RestoreUnavailableMessage));
        }

        /// <summary>
        /// Restores an encrypted full vault backup using <paramref name="encryptionKey"/>.
        /// </summary>
        public Task<RestoreResult> RestoreFromBackupAsync(
            string backupId,
            byte[] encryptionKey,
            bool preserveExisting = true)
        {
            _logger.Log(RestoreUnavailableMessage);
            return Task.FromResult(RestoreResult.Failure(RestoreUnavailableMessage));
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
                if (incremental)
                {
                    _logger.Log(IncrementalBackupsUnavailableMessage);
                    return false;
                }

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

        private async Task<ValidatedBackup> LoadValidatedBackupAsync(string backupId)
        {
            ValidateBackupId(backupId);
            var backupDirectory = GetPathWithinRoot(_backupPath, backupId);
            var manifestPath = GetPathWithinRoot(backupDirectory, "manifest.json");
            if (!Directory.Exists(backupDirectory) || !File.Exists(manifestPath))
            {
                throw new InvalidDataException($"Backup '{backupId}' or its manifest was not found.");
            }

            var manifestData = JsonSerializer.Deserialize<BackupManifestData>(
                await File.ReadAllTextAsync(manifestPath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (manifestData == null ||
                string.IsNullOrWhiteSpace(manifestData.BackupId) ||
                !string.Equals(manifestData.BackupId, backupId, GetPathStringComparison()) ||
                manifestData.BackupType == null ||
                manifestData.IsIncremental == null ||
                manifestData.FileCount == null ||
                manifestData.TotalSize == null ||
                manifestData.IsEncrypted == null ||
                manifestData.FileCount < 0 ||
                manifestData.TotalSize < 0)
            {
                throw new InvalidDataException($"Backup '{backupId}' has an invalid manifest.");
            }

            var manifest = new ValidatedBackupManifest
            {
                BackupId = manifestData.BackupId,
                BackupType = manifestData.BackupType.Value,
                IsIncremental = manifestData.IsIncremental.Value,
                BaseBackupId = manifestData.BaseBackupId,
                FileCount = manifestData.FileCount.Value,
                TotalSize = manifestData.TotalSize.Value,
                IsEncrypted = manifestData.IsEncrypted.Value
            };

            var files = EnumerateFilesWithoutReparsePoints(backupDirectory)
                .Where(file => !string.Equals(
                    Path.GetFullPath(file),
                    Path.GetFullPath(manifestPath),
                    GetPathStringComparison()))
                .Select(file => new ValidatedBackupFile
                {
                    SourcePath = file,
                    RelativePath = GetValidatedRelativePath(backupDirectory, file),
                    Size = new FileInfo(file).Length
                })
                .ToList();

            if (files.Count != manifest.FileCount || files.Sum(file => file.Size) != manifest.TotalSize)
            {
                throw new InvalidDataException($"Backup '{backupId}' failed size/count verification.");
            }

            return new ValidatedBackup { Manifest = manifest, Files = files };
        }

        private static void EnsureFullBackupIsSupported(ValidatedBackupManifest manifest)
        {
            if (manifest.IsIncremental ||
                manifest.BackupType != BackupType.Full ||
                !string.IsNullOrWhiteSpace(manifest.BaseBackupId))
            {
                throw new NotSupportedException(IncrementalBackupsUnavailableMessage);
            }
        }

        private static IEnumerable<string> EnumerateFilesWithoutReparsePoints(string rootDirectory)
        {
            return Directory.EnumerateFiles(
                rootDirectory,
                "*",
                new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    AttributesToSkip = FileAttributes.ReparsePoint,
                    IgnoreInaccessible = false
                });
        }

        private static string GetValidatedRelativePath(string rootDirectory, string path)
        {
            var relativePath = Path.GetRelativePath(Path.GetFullPath(rootDirectory), Path.GetFullPath(path));
            _ = GetPathWithinRoot(rootDirectory, relativePath);
            return relativePath;
        }

        private static string GetPathWithinRoot(string rootDirectory, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            {
                throw new InvalidDataException("Restore path must be relative.");
            }

            var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootDirectory));
            var candidate = Path.GetFullPath(Path.Combine(root, relativePath));
            if (!candidate.StartsWith(root + Path.DirectorySeparatorChar, GetPathStringComparison()))
            {
                throw new InvalidDataException("Restore path escapes its expected root.");
            }

            return candidate;
        }

        private static void ValidateBackupId(string backupId)
        {
            if (string.IsNullOrWhiteSpace(backupId) ||
                Path.IsPathRooted(backupId) ||
                backupId is "." or ".." ||
                backupId.IndexOfAny(new[] { '/', '\\', ':' }) >= 0)
            {
                throw new InvalidDataException("Backup ID must be a simple directory name.");
            }
        }

        private static StringComparison GetPathStringComparison() =>
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

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

        private const string IncrementalBackupsUnavailableMessage =
            "Incremental vault backups are disabled until deletion tracking and chain-aware retention are implemented; use full backups.";

        private const string EncryptedBackupsUnavailableMessage =
            "Encrypted vault backups are disabled until backup identity and paths are cryptographically authenticated.";

        private const string RestoreUnavailableMessage =
            "Vault restore is disabled until backup identity is authenticated and the live-vault commit is transactional.";

        private sealed class BackupManifestData
        {
            public string BackupId { get; set; }
            public BackupType? BackupType { get; set; }
            public bool? IsIncremental { get; set; }
            public string BaseBackupId { get; set; }
            public int? FileCount { get; set; }
            public long? TotalSize { get; set; }
            public bool? IsEncrypted { get; set; }
        }

        private sealed class ValidatedBackupManifest
        {
            public string BackupId { get; set; }
            public BackupType BackupType { get; set; }
            public bool IsIncremental { get; set; }
            public string BaseBackupId { get; set; }
            public int FileCount { get; set; }
            public long TotalSize { get; set; }
            public bool IsEncrypted { get; set; }
        }

        private sealed class ValidatedBackupFile
        {
            public string SourcePath { get; set; }
            public string RelativePath { get; set; }
            public long Size { get; set; }
        }

        private sealed class ValidatedBackup
        {
            public ValidatedBackupManifest Manifest { get; set; }
            public List<ValidatedBackupFile> Files { get; set; }
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
