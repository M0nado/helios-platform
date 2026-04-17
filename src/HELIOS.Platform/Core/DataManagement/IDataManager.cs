using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.DataManagement
{
    /// <summary>
    /// Interface for data management operations including import, export, migration, and backup
    /// </summary>
    public interface IDataManager
    {
        // Recent Files and Projects
        Task<IEnumerable<RecentItem>> GetRecentFilesAsync(int count = 10);
        Task<IEnumerable<RecentItem>> GetRecentProjectsAsync(int count = 10);
        Task AddRecentItemAsync(RecentItem item);
        Task ClearRecentItemsAsync();

        // Export Operations
        Task<string> ExportToJsonAsync(string sourcePath, ExportOptions options = null);
        Task<string> ExportToCsvAsync(string sourcePath, ExportOptions options = null);
        Task<string> ExportToXmlAsync(string sourcePath, ExportOptions options = null);
        Task<string> ExportDataAsync(string sourcePath, ExportFormat format, ExportOptions options = null);

        // Import Operations
        Task<bool> ImportFromJsonAsync(string filePath, ImportOptions options = null);
        Task<bool> ImportFromCsvAsync(string filePath, ImportOptions options = null);
        Task<bool> ImportFromXmlAsync(string filePath, ImportOptions options = null);
        Task<bool> ImportDataAsync(string filePath, ImportFormat format, ImportOptions options = null);

        // Migration
        Task<bool> MigrateFromOldVersionAsync(string oldVersionPath);
        Task<IEnumerable<MigrationStep>> GetMigrationStepsAsync(string fromVersion, string toVersion);
        Task<bool> ExecuteMigrationAsync(string fromVersion, string toVersion, IProgress<int> progress = null);

        // Backup and Recovery
        Task<string> CreateBackupAsync(string sourcePath, BackupOptions options = null);
        Task<bool> RestoreBackupAsync(string backupPath, string targetPath);
        Task<IEnumerable<BackupRecord>> GetBackupHistoryAsync();
        Task<bool> DeleteBackupAsync(string backupId);

        // Data Integrity
        Task<DataIntegrityReport> CheckDataIntegrityAsync(string dataPath);
        Task<bool> RepairDataAsync(string dataPath);

        // Data Sync and Replication
        Task<bool> SyncDataAsync(string sourcePath, string destinationPath);
        Task<bool> ReplicateDataAsync(string sourcePath, string destinationPath);
        Task<bool> StartContinuousSyncAsync(string sourcePath, string destinationPath);
        Task StopContinuousSyncAsync();

        // Data Archival
        Task<string> ArchiveDataAsync(string sourcePath, ArchiveOptions options = null);
        Task<bool> RestoreArchiveAsync(string archivePath, string targetPath);

        // Data Retention
        Task SetRetentionPolicyAsync(RetentionPolicy policy);
        Task<RetentionPolicy> GetRetentionPolicyAsync();
        Task<bool> ApplyRetentionPolicyAsync();

        // Data Recovery Tools
        Task<IEnumerable<RecoverableItem>> ScanForRecoverableDataAsync(string path);
        Task<bool> RecoverDataAsync(string itemId, string targetPath);
    }

    /// <summary>
    /// Recent file or project item
    /// </summary>
    public class RecentItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public DateTime AccessTime { get; set; }
        public ItemType Type { get; set; }
    }

    /// <summary>
    /// Export options
    /// </summary>
    public class ExportOptions
    {
        public bool IncludeMetadata { get; set; }
        public bool Compress { get; set; }
        public string OutputPath { get; set; }
        public bool Overwrite { get; set; }
        public Dictionary<string, object> CustomOptions { get; set; }
    }

    /// <summary>
    /// Import options
    /// </summary>
    public class ImportOptions
    {
        public bool MergeWithExisting { get; set; }
        public bool ValidateBeforeImport { get; set; }
        public bool BackupBeforeImport { get; set; }
        public Dictionary<string, object> CustomOptions { get; set; }
    }

    /// <summary>
    /// Migration step for version upgrades
    /// </summary>
    public class MigrationStep
    {
        public int Order { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Func<Task<bool>> Action { get; set; }
        public bool IsOptional { get; set; }
    }

    /// <summary>
    /// Backup options
    /// </summary>
    public class BackupOptions
    {
        public bool Compress { get; set; }
        public bool Encrypt { get; set; }
        public string EncryptionKey { get; set; }
        public bool IncludeMetadata { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// Backup record
    /// </summary>
    public class BackupRecord
    {
        public string BackupId { get; set; }
        public DateTime CreatedTime { get; set; }
        public long SizeInBytes { get; set; }
        public string Description { get; set; }
        public bool IsEncrypted { get; set; }
        public string BackupPath { get; set; }
    }

    /// <summary>
    /// Data integrity report
    /// </summary>
    public class DataIntegrityReport
    {
        public bool IsValid { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }
        public DateTime CheckTime { get; set; }
    }

    /// <summary>
    /// Archive options
    /// </summary>
    public class ArchiveOptions
    {
        public bool Compress { get; set; }
        public int CompressionLevel { get; set; }
        public bool Encrypt { get; set; }
        public DateTime? ArchiveDate { get; set; }
    }

    /// <summary>
    /// Data retention policy
    /// </summary>
    public class RetentionPolicy
    {
        public int DaysToRetain { get; set; }
        public bool ArchiveBeforeDelete { get; set; }
        public DateTime LastApplied { get; set; }
        public bool AutoApplyOnSchedule { get; set; }
        public string ScheduleExpression { get; set; }
    }

    /// <summary>
    /// Recoverable item
    /// </summary>
    public class RecoverableItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime DeletedTime { get; set; }
        public long SizeInBytes { get; set; }
        public string OriginalPath { get; set; }
    }

    /// <summary>
    /// Export format
    /// </summary>
    public enum ExportFormat
    {
        Json,
        Csv,
        Xml,
        Binary
    }

    /// <summary>
    /// Import format
    /// </summary>
    public enum ImportFormat
    {
        Json,
        Csv,
        Xml,
        Binary
    }

    /// <summary>
    /// Item type
    /// </summary>
    public enum ItemType
    {
        File,
        Project,
        Folder
    }
}
