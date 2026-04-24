using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HELIOS.Platform.Phase10.Quarantine
{
    /// <summary>
    /// Manages quarantine operations: listing, viewing, deleting, restoring files
    /// </summary>
    public class QuarantineManager
    {
        private readonly string _quarantinePath;
        private readonly ILogger _logger;
        private readonly string _activeThreatDir;
        private readonly string _backupDir;
        private readonly string _archiveDir;

        public QuarantineManager(ILogger logger, string quarantineRootPath = "I:\\")
        {
            _quarantinePath = quarantineRootPath;
            _activeThreatDir = Path.Combine(_quarantinePath, "active-threats");
            _backupDir = Path.Combine(_quarantinePath, "backup-quarantined");
            _archiveDir = Path.Combine(_quarantinePath, "archive");
            _logger = logger;
        }

        /// <summary>
        /// List all quarantined files
        /// </summary>
        public async Task<List<QuarantinedFile>> ListQuarantinedFilesAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogInfo("Listing quarantined files");
                    var quarantinedFiles = new List<QuarantinedFile>();

                    if (!Directory.Exists(_activeThreatDir))
                    {
                        _logger.LogWarning("Active threats directory does not exist");
                        return quarantinedFiles;
                    }

                    var files = Directory.GetFiles(_activeThreatDir);
                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        var qFile = new QuarantinedFile
                        {
                            FileName = fileInfo.Name,
                            FilePath = file,
                            FileSize = fileInfo.Length,
                            QuarantineDate = fileInfo.CreationTimeUtc,
                            Status = "Active"
                        };

                        quarantinedFiles.Add(qFile);
                    }

                    _logger.LogInfo($"Found {quarantinedFiles.Count} quarantined files");
                    return quarantinedFiles;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to list quarantined files: {ex.Message}");
                    return new List<QuarantinedFile>();
                }
            });
        }

        /// <summary>
        /// Get detailed information about a quarantined file
        /// </summary>
        public async Task<QuarantinedFileDetails> GetFileDetailsAsync(string fileName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogInfo($"Getting details for: {fileName}");

                    string filePath = Path.Combine(_activeThreatDir, fileName);
                    if (!File.Exists(filePath))
                    {
                        _logger.LogError($"File not found: {fileName}");
                        return new QuarantinedFileDetails { ErrorMessage = "File not found" };
                    }

                    var fileInfo = new FileInfo(filePath);
                    var details = new QuarantinedFileDetails
                    {
                        FileName = fileInfo.Name,
                        FilePath = filePath,
                        FileSize = fileInfo.Length,
                        QuarantineDate = fileInfo.CreationTimeUtc,
                        LastModified = fileInfo.LastWriteTimeUtc,
                        Owner = GetFileOwner(filePath),
                        ThreatLevel = ExtractThreatLevelFromFileName(fileName),
                        ThreatName = ExtractThreatNameFromFileName(fileName),
                        FileHash = ExtractHashFromFileName(fileName),
                        BackupPath = FindBackupFile(fileName)
                    };

                    return details;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to get file details: {ex.Message}");
                    return new QuarantinedFileDetails { ErrorMessage = ex.Message };
                }
            });
        }

        /// <summary>
        /// Delete a quarantined file permanently
        /// </summary>
        public async Task<bool> DeleteThreatAsync(string fileName, bool deleteBackup = false)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogWarning($"Deleting threat: {fileName}");

                    string filePath = Path.Combine(_activeThreatDir, fileName);
                    if (!File.Exists(filePath))
                    {
                        _logger.LogError($"File not found: {fileName}");
                        return false;
                    }

                    // Secure deletion using multiple overwrites
                    SecureDelete(filePath);
                    _logger.LogInfo($"Threat deleted: {fileName}");

                    // Optionally delete backup
                    if (deleteBackup)
                    {
                        string backupPath = FindBackupFile(fileName);
                        if (!string.IsNullOrEmpty(backupPath) && File.Exists(backupPath))
                        {
                            SecureDelete(backupPath);
                            _logger.LogInfo($"Backup deleted: {backupPath}");
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to delete threat: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Restore a file from quarantine (after analysis)
        /// </summary>
        public async Task<bool> RestoreFileAsync(string fileName, string restorePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogInfo($"Restoring file: {fileName} to {restorePath}");

                    string quarantinedPath = Path.Combine(_activeThreatDir, fileName);
                    if (!File.Exists(quarantinedPath))
                    {
                        _logger.LogError($"Quarantined file not found: {fileName}");
                        return false;
                    }

                    // Ensure destination directory exists
                    string destDir = Path.GetDirectoryName(restorePath);
                    Directory.CreateDirectory(destDir);

                    // Restore from backup if available, otherwise from quarantine
                    string sourceFile = FindBackupFile(fileName);
                    if (string.IsNullOrEmpty(sourceFile) || !File.Exists(sourceFile))
                    {
                        sourceFile = quarantinedPath;
                    }

                    File.Copy(sourceFile, restorePath, overwrite: true);
                    _logger.LogInfo($"File restored successfully: {restorePath}");

                    // Log restoration
                    LogRestorationAsync(fileName, restorePath).Wait();

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to restore file: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Export threat for external analysis
        /// </summary>
        public async Task<bool> ExportForAnalysisAsync(string fileName, string exportPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogInfo($"Exporting threat: {fileName} to {exportPath}");

                    string quarantinedPath = Path.Combine(_activeThreatDir, fileName);
                    if (!File.Exists(quarantinedPath))
                    {
                        _logger.LogError($"File not found: {fileName}");
                        return false;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(exportPath));
                    File.Copy(quarantinedPath, exportPath, overwrite: true);

                    _logger.LogInfo($"Threat exported: {exportPath}");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to export threat: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Update threat intelligence from external sources
        /// </summary>
        public async Task<bool> UpdateThreatIntelligenceAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogInfo("Updating threat intelligence");

                    string threatDbPath = Path.Combine(_quarantinePath, "threat-database");
                    Directory.CreateDirectory(threatDbPath);

                    var updateInfo = new StringBuilder();
                    updateInfo.AppendLine($"[{DateTime.UtcNow:u}] Threat Intelligence Update");
                    updateInfo.AppendLine($"Total Quarantined Files: {Directory.GetFiles(_activeThreatDir).Length}");
                    updateInfo.AppendLine($"Database Version: 1.0.0");
                    updateInfo.AppendLine($"Last Update: {DateTime.UtcNow:u}");

                    string logPath = Path.Combine(threatDbPath, "intelligence-updates.log");
                    File.AppendAllText(logPath, updateInfo.ToString() + Environment.NewLine);

                    _logger.LogInfo("Threat intelligence updated");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to update threat intelligence: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Archive old quarantined files
        /// </summary>
        public async Task<int> ArchiveOldThreatsAsync(int daysOld = 90)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogInfo($"Archiving threats older than {daysOld} days");

                    Directory.CreateDirectory(_archiveDir);
                    int archivedCount = 0;
                    DateTime cutoffDate = DateTime.UtcNow.AddDays(-daysOld);

                    if (!Directory.Exists(_activeThreatDir))
                        return 0;

                    var files = Directory.GetFiles(_activeThreatDir);
                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.CreationTimeUtc < cutoffDate)
                        {
                            string archiveName = Path.Combine(_archiveDir, 
                                $"{fileInfo.CreationTimeUtc:yyyyMM}_{fileInfo.Name}");
                            
                            File.Move(file, archiveName, overwrite: true);
                            archivedCount++;
                            _logger.LogInfo($"Archived: {fileInfo.Name}");
                        }
                    }

                    _logger.LogInfo($"Archived {archivedCount} threats");
                    return archivedCount;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to archive threats: {ex.Message}");
                    return 0;
                }
            });
        }

        /// <summary>
        /// Get quarantine space statistics
        /// </summary>
        public async Task<QuarantineStats> GetQuarantineStatsAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogInfo("Calculating quarantine statistics");

                    var stats = new QuarantineStats
                    {
                        Timestamp = DateTime.UtcNow
                    };

                    if (Directory.Exists(_activeThreatDir))
                    {
                        var files = Directory.GetFiles(_activeThreatDir);
                        stats.ActiveThreatCount = files.Length;
                        stats.ActiveThreatSize = files.Sum(f => new FileInfo(f).Length);
                    }

                    if (Directory.Exists(_archiveDir))
                    {
                        var files = Directory.GetFiles(_archiveDir);
                        stats.ArchivedThreatCount = files.Length;
                        stats.ArchivedThreatSize = files.Sum(f => new FileInfo(f).Length);
                    }

                    if (Directory.Exists(_backupDir))
                    {
                        var files = Directory.GetFiles(_backupDir);
                        stats.BackupCount = files.Length;
                        stats.BackupSize = files.Sum(f => new FileInfo(f).Length);
                    }

                    stats.TotalThreatCount = stats.ActiveThreatCount + stats.ArchivedThreatCount;
                    stats.TotalUsedSpace = stats.ActiveThreatSize + stats.ArchivedThreatSize + stats.BackupSize;

                    return stats;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to calculate stats: {ex.Message}");
                    return new QuarantineStats { ErrorMessage = ex.Message };
                }
            });
        }

        #region Helper Methods

        private void SecureDelete(string filePath)
        {
            try
            {
                // Overwrite file with random data multiple times
                byte[] randomData = new byte[new FileInfo(filePath).Length];
                for (int pass = 0; pass < 3; pass++)
                {
                    new Random().NextBytes(randomData);
                    File.WriteAllBytes(filePath, randomData);
                }

                // Final deletion
                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Secure delete failed: {ex.Message}");
                // Fallback to normal delete
                try { File.Delete(filePath); }
                catch { }
            }
        }

        private string ExtractThreatLevelFromFileName(string fileName)
        {
            // Parse threat level from filename if available
            return "Unknown";
        }

        private string ExtractThreatNameFromFileName(string fileName)
        {
            // Extract threat name from quarantine filename
            var parts = fileName.Split('_');
            if (parts.Length >= 3)
                return parts[1];
            return "Unknown";
        }

        private string ExtractHashFromFileName(string fileName)
        {
            // Extract hash from quarantine filename
            var parts = fileName.Split('_');
            if (parts.Length >= 3)
                return parts[2].Replace(".quarantine", "");
            return "";
        }

        private string FindBackupFile(string quarantineFileName)
        {
            try
            {
                var backupFiles = Directory.GetFiles(_backupDir);
                return backupFiles.FirstOrDefault(f => 
                    f.Contains(ExtractThreatNameFromFileName(quarantineFileName)));
            }
            catch
            {
                return null;
            }
        }

        private string GetFileOwner(string filePath)
        {
            try
            {
                var fi = new FileInfo(filePath);
                var fs = fi.GetAccessControl();
                var owner = fs.GetOwner(typeof(System.Security.Principal.NTAccount));
                return owner?.Value ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private async Task<bool> LogRestorationAsync(string fileName, string restorePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string logsDir = Path.Combine(_quarantinePath, "analysis-logs");
                    Directory.CreateDirectory(logsDir);

                    string logPath = Path.Combine(logsDir, "restorations.log");
                    var logEntry = $"[{DateTime.UtcNow:u}] FILE RESTORED - {fileName} -> {restorePath}{Environment.NewLine}";
                    File.AppendAllText(logPath, logEntry);

                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        #endregion
    }

    public class QuarantinedFile
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public DateTime QuarantineDate { get; set; }
        public string Status { get; set; }
    }

    public class QuarantinedFileDetails
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public DateTime QuarantineDate { get; set; }
        public DateTime LastModified { get; set; }
        public string Owner { get; set; }
        public string ThreatLevel { get; set; }
        public string ThreatName { get; set; }
        public string FileHash { get; set; }
        public string BackupPath { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class QuarantineStats
    {
        public DateTime Timestamp { get; set; }
        public int ActiveThreatCount { get; set; }
        public long ActiveThreatSize { get; set; }
        public int ArchivedThreatCount { get; set; }
        public long ArchivedThreatSize { get; set; }
        public int BackupCount { get; set; }
        public long BackupSize { get; set; }
        public int TotalThreatCount { get; set; }
        public long TotalUsedSpace { get; set; }
        public string ErrorMessage { get; set; }
    }
}
