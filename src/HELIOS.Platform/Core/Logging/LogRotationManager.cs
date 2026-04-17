using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Logging
{
    /// <summary>
    /// Manages log file rotation, archival, and cleanup
    /// </summary>
    public class LogRotationManager
    {
        private static readonly ILogger _logger = Log.ForContext<LogRotationManager>();
        private static readonly string LogDirectory = LoggerConfiguration.GetLogDirectory();
        private static readonly string ArchiveDirectory = LoggerConfiguration.GetArchiveDirectory();

        /// <summary>
        /// Configuration for log rotation
        /// </summary>
        public class RotationConfig
        {
            /// <summary>Maximum log file size in bytes (default: 100 MB)</summary>
            public long MaxFileSizeBytes { get; set; } = 100 * 1024 * 1024;

            /// <summary>Maximum number of retained log files (default: 30)</summary>
            public int MaxRetainedFiles { get; set; } = 30;

            /// <summary>Days to keep logs before archival (default: 7)</summary>
            public int DaysBeforeArchival { get; set; } = 7;

            /// <summary>Days to keep archived logs (default: 90)</summary>
            public int DaysToKeepArchived { get; set; } = 90;

            /// <summary>Enable automatic compression of archived logs (default: true)</summary>
            public bool CompressArchivedLogs { get; set; } = true;
        }

        /// <summary>
        /// Perform log rotation
        /// </summary>
        public static void RotateLogs(RotationConfig config = null)
        {
            config ??= new RotationConfig();

            Task.Run(async () =>
            {
                try
                {
                    // Clean up old files
                    CleanOldLogFiles(config.MaxRetainedFiles);

                    // Archive old logs
                    ArchiveOldLogs(config.DaysBeforeArchival);

                    // Clean old archives
                    CleanOldArchives(config.DaysToKeepArchived);

                    _logger.Information("Log rotation completed successfully");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error during log rotation");
                }
            });
        }

        /// <summary>
        /// Clean up old log files exceeding the retained count
        /// </summary>
        private static void CleanOldLogFiles(int maxRetainedFiles)
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                    return;

                var logFiles = Directory.GetFiles(LogDirectory, "*.txt", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(f => File.GetCreationTimeUtc(f))
                    .ToList();

                if (logFiles.Count > maxRetainedFiles)
                {
                    var filesToDelete = logFiles.Skip(maxRetainedFiles).ToList();
                    foreach (var file in filesToDelete)
                    {
                        try
                        {
                            File.Delete(file);
                            _logger.Debug("Deleted old log file: {File}", Path.GetFileName(file));
                        }
                        catch (Exception ex)
                        {
                            _logger.Warning(ex, "Failed to delete log file: {File}", file);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error cleaning old log files");
            }
        }

        /// <summary>
        /// Archive old log files
        /// </summary>
        private static void ArchiveOldLogs(int daysToKeep)
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                    return;

                Directory.CreateDirectory(ArchiveDirectory);

                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                var logFiles = Directory.GetFiles(LogDirectory, "*.txt", SearchOption.TopDirectoryOnly)
                    .Where(f =>
                    {
                        try
                        {
                            return File.GetCreationTimeUtc(f) < cutoffDate;
                        }
                        catch
                        {
                            return false;
                        }
                    })
                    .ToList();

                foreach (var file in logFiles)
                {
                    try
                    {
                        var fileName = Path.GetFileName(file);
                        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
                        var archivePath = Path.Combine(ArchiveDirectory, $"{timestamp}_{fileName}");
                        File.Move(file, archivePath, true);
                        _logger.Debug("Archived log file: {File}", fileName);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning(ex, "Failed to archive log file: {File}", file);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error archiving old logs");
            }
        }

        /// <summary>
        /// Clean old archived log files
        /// </summary>
        private static void CleanOldArchives(int daysToKeep)
        {
            try
            {
                if (!Directory.Exists(ArchiveDirectory))
                    return;

                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                var archiveFiles = Directory.GetFiles(ArchiveDirectory, "*", SearchOption.TopDirectoryOnly)
                    .Where(f =>
                    {
                        try
                        {
                            return File.GetCreationTimeUtc(f) < cutoffDate;
                        }
                        catch
                        {
                            return false;
                        }
                    })
                    .ToList();

                foreach (var file in archiveFiles)
                {
                    try
                    {
                        File.Delete(file);
                        _logger.Debug("Deleted old archive: {File}", Path.GetFileName(file));
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning(ex, "Failed to delete archive: {File}", file);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error cleaning old archives");
            }
        }

        /// <summary>
        /// Get log file statistics
        /// </summary>
        public static LogStatistics GetLogStatistics()
        {
            try
            {
                var stats = new LogStatistics();

                if (Directory.Exists(LogDirectory))
                {
                    var logFiles = Directory.GetFiles(LogDirectory, "*.txt", SearchOption.TopDirectoryOnly);
                    stats.LogFileCount = logFiles.Length;
                    stats.TotalLogSizeBytes = logFiles.Sum(f => new FileInfo(f).Length);
                    stats.OldestLogDate = logFiles.Any() 
                        ? logFiles.Select(f => File.GetCreationTimeUtc(f)).Min() 
                        : DateTime.UtcNow;
                    stats.NewestLogDate = logFiles.Any() 
                        ? logFiles.Select(f => File.GetCreationTimeUtc(f)).Max() 
                        : DateTime.UtcNow;
                }

                if (Directory.Exists(ArchiveDirectory))
                {
                    var archiveFiles = Directory.GetFiles(ArchiveDirectory, "*", SearchOption.TopDirectoryOnly);
                    stats.ArchiveFileCount = archiveFiles.Length;
                    stats.TotalArchiveSizeBytes = archiveFiles.Sum(f => new FileInfo(f).Length);
                }

                return stats;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting log statistics");
                return new LogStatistics();
            }
        }
    }

    /// <summary>
    /// Log statistics information
    /// </summary>
    public class LogStatistics
    {
        public int LogFileCount { get; set; }
        public long TotalLogSizeBytes { get; set; }
        public DateTime OldestLogDate { get; set; }
        public DateTime NewestLogDate { get; set; }
        public int ArchiveFileCount { get; set; }
        public long TotalArchiveSizeBytes { get; set; }

        public double TotalLogSizeMB => TotalLogSizeBytes / (1024.0 * 1024.0);
        public double TotalArchiveSizeMB => TotalArchiveSizeBytes / (1024.0 * 1024.0);
    }
}
