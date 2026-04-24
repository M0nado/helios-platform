using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HELIOS.Platform.Phase10.Quarantine
{
    /// <summary>
    /// Captures suspicious files and isolates them in quarantine
    /// </summary>
    public class ThreatCapture
    {
        private readonly string _quarantinePath;
        private readonly ILogger _logger;
        private readonly string _activeThreatDir;
        private readonly string _backupDir;
        private readonly string _analysisLogsDir;

        public ThreatCapture(ILogger logger, string quarantineRootPath = "I:\\")
        {
            _quarantinePath = quarantineRootPath;
            _activeThreatDir = Path.Combine(_quarantinePath, "active-threats");
            _backupDir = Path.Combine(_quarantinePath, "backup-quarantined");
            _analysisLogsDir = Path.Combine(_quarantinePath, "analysis-logs");
            _logger = logger;
        }

        /// <summary>
        /// Capture a suspicious file and move it to quarantine
        /// </summary>
        public async Task<ThreatCaptureResult> CaptureThreatAsync(string filePath, string threatName = null)
        {
            var result = new ThreatCaptureResult { FilePath = filePath, Timestamp = DateTime.UtcNow };

            try
            {
                if (!File.Exists(filePath))
                {
                    result.IsSuccessful = false;
                    result.ErrorMessage = $"File not found: {filePath}";
                    _logger.LogError(result.ErrorMessage);
                    return result;
                }

                _logger.LogInfo($"Capturing threat: {filePath}");

                // Extract file metadata
                result.FileMetadata = await ExtractFileMetadataAsync(filePath);

                // Compute SHA256 hash
                result.FileHash = await ComputeFileHashAsync(filePath);

                // Create quarantine filename with metadata
                string quarantineFileName = CreateQuarantineFileName(threatName ?? "unknown", result.FileHash);
                string quarantinePath = Path.Combine(_activeThreatDir, quarantineFileName);

                // Create backup copy
                string backupPath = Path.Combine(_backupDir, Path.GetFileName(filePath) + ".bak");
                await BackupOriginalFileAsync(filePath, backupPath);
                result.BackupPath = backupPath;

                // Move file to quarantine
                Directory.CreateDirectory(_activeThreatDir);
                if (File.Exists(quarantinePath))
                {
                    File.Delete(quarantinePath);
                }
                File.Move(filePath, quarantinePath);
                result.QuarantinePath = quarantinePath;

                // Log detection
                await LogDetectionAsync(result);

                // Notify user
                NotifyUser(result);

                result.IsSuccessful = true;
                _logger.LogInfo($"Threat captured successfully: {quarantinePath}");
            }
            catch (Exception ex)
            {
                result.IsSuccessful = false;
                result.ErrorMessage = $"Failed to capture threat: {ex.Message}";
                _logger.LogError(result.ErrorMessage);
            }

            return result;
        }

        /// <summary>
        /// Capture multiple threats in batch
        /// </summary>
        public async Task<List<ThreatCaptureResult>> CaptureThreatsBatchAsync(List<string> filePaths)
        {
            _logger.LogInfo($"Capturing {filePaths.Count} threats in batch");
            var results = new List<ThreatCaptureResult>();

            foreach (var filePath in filePaths)
            {
                var result = await CaptureThreatAsync(filePath);
                results.Add(result);
            }

            return results;
        }

        /// <summary>
        /// Extract metadata from file
        /// </summary>
        private async Task<FileMetadata> ExtractFileMetadataAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    
                    return new FileMetadata
                    {
                        FileName = fileInfo.Name,
                        FilePath = filePath,
                        FileSize = fileInfo.Length,
                        CreatedTime = fileInfo.CreationTimeUtc,
                        ModifiedTime = fileInfo.LastWriteTimeUtc,
                        FileExtension = fileInfo.Extension,
                        Owner = GetFileOwner(filePath),
                        Attributes = fileInfo.Attributes.ToString()
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to extract metadata: {ex.Message}");
                    return null;
                }
            });
        }

        /// <summary>
        /// Compute SHA256 hash of file
        /// </summary>
        private async Task<string> ComputeFileHashAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var sha256 = SHA256.Create())
                    {
                        using (var stream = File.OpenRead(filePath))
                        {
                            var hash = sha256.ComputeHash(stream);
                            return Convert.ToHexString(hash);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to compute hash: {ex.Message}");
                    return "hash_error";
                }
            });
        }

        /// <summary>
        /// Create quarantine filename with metadata
        /// </summary>
        private string CreateQuarantineFileName(string threatName, string hash)
        {
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            string shortHash = hash.Substring(0, 8).ToUpper();
            string sanitizedThreatName = System.Text.RegularExpressions.Regex.Replace(threatName, @"[^a-zA-Z0-9_-]", "_");
            
            return $"{timestamp}_{sanitizedThreatName}_{shortHash}.quarantine";
        }

        /// <summary>
        /// Backup original file
        /// </summary>
        private async Task<bool> BackupOriginalFileAsync(string source, string destination)
        {
            return await Task.Run(() =>
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    File.Copy(source, destination, overwrite: true);
                    _logger.LogInfo($"Backup created: {destination}");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to backup file: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Log threat detection
        /// </summary>
        private async Task<bool> LogDetectionAsync(ThreatCaptureResult result)
        {
            return await Task.Run(() =>
            {
                try
                {
                    Directory.CreateDirectory(_analysisLogsDir);
                    
                    string logFileName = $"detection_{result.Timestamp:yyyyMMdd_HHmmss}.log";
                    string logPath = Path.Combine(_analysisLogsDir, logFileName);

                    var logEntry = new StringBuilder();
                    logEntry.AppendLine($"[{result.Timestamp:u}] THREAT DETECTED");
                    logEntry.AppendLine($"Original Path: {result.FilePath}");
                    logEntry.AppendLine($"Quarantine Path: {result.QuarantinePath}");
                    logEntry.AppendLine($"File Hash (SHA256): {result.FileHash}");
                    logEntry.AppendLine($"Backup Path: {result.BackupPath}");
                    
                    if (result.FileMetadata != null)
                    {
                        logEntry.AppendLine($"File Name: {result.FileMetadata.FileName}");
                        logEntry.AppendLine($"File Size: {result.FileMetadata.FileSize} bytes");
                        logEntry.AppendLine($"Owner: {result.FileMetadata.Owner}");
                        logEntry.AppendLine($"Created: {result.FileMetadata.CreatedTime:u}");
                        logEntry.AppendLine($"Modified: {result.FileMetadata.ModifiedTime:u}");
                    }

                    File.AppendAllText(logPath, logEntry.ToString() + Environment.NewLine);
                    _logger.LogInfo($"Detection logged: {logPath}");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to log detection: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Notify user about threat capture
        /// </summary>
        private void NotifyUser(ThreatCaptureResult result)
        {
            try
            {
                string title = "Threat Captured";
                string message = $"Threat detected and quarantined:\n\n" +
                                 $"File: {result.FileMetadata?.FileName}\n" +
                                 $"Size: {result.FileMetadata?.FileSize} bytes\n" +
                                 $"Quarantine: {result.QuarantinePath}";
                
                _logger.LogInfo($"User notification: {title} - {message}");
                
                // In production, show Windows notification or system tray message
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to notify user: {ex.Message}");
            }
        }

        /// <summary>
        /// Get file owner
        /// </summary>
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
    }

    /// <summary>
    /// Result of threat capture operation
    /// </summary>
    public class ThreatCaptureResult
    {
        public string FilePath { get; set; }
        public string QuarantinePath { get; set; }
        public string BackupPath { get; set; }
        public string FileHash { get; set; }
        public FileMetadata FileMetadata { get; set; }
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// File metadata information
    /// </summary>
    public class FileMetadata
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime ModifiedTime { get; set; }
        public string FileExtension { get; set; }
        public string Owner { get; set; }
        public string Attributes { get; set; }
    }
}
