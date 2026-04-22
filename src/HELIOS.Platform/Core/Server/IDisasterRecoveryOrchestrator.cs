using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Server
{
    /// <summary>
    /// Disaster recovery orchestrator interface for backup and recovery operations.
    /// </summary>
    public interface IDisasterRecoveryOrchestrator
    {
        /// <summary>
        /// Initiates a backup operation.
        /// </summary>
        Task<BackupOperation> InitiateBackupAsync(string backupName, BackupType backupType, List<string> targets);

        /// <summary>
        /// Gets the status of a backup operation.
        /// </summary>
        Task<BackupOperation?> GetBackupStatusAsync(string backupId);

        /// <summary>
        /// Lists all backups.
        /// </summary>
        Task<List<BackupOperation>> ListBackupsAsync(int limit = 100);

        /// <summary>
        /// Initiates recovery from a backup.
        /// </summary>
        Task<RecoveryOperation> InitiateRecoveryAsync(string backupId, RecoveryType recoveryType, List<string> targetResources);

        /// <summary>
        /// Gets the status of a recovery operation.
        /// </summary>
        Task<RecoveryOperation?> GetRecoveryStatusAsync(string recoveryId);

        /// <summary>
        /// Lists all recovery operations.
        /// </summary>
        Task<List<RecoveryOperation>> ListRecoveriesAsync(int limit = 100);

        /// <summary>
        /// Configures recovery point objective (RPO) for a resource.
        /// </summary>
        Task<bool> ConfigureRpoAsync(string resourceId, int rpoMinutes);

        /// <summary>
        /// Gets configured RPO for a resource.
        /// </summary>
        Task<int> GetRpoAsync(string resourceId);

        /// <summary>
        /// Registers a backup destination (local, cloud, etc.).
        /// </summary>
        Task<bool> RegisterBackupDestinationAsync(string destinationId, string destinationType, string connectionString);

        /// <summary>
        /// Performs multi-region recovery setup.
        /// </summary>
        Task<bool> SetupMultiRegionRecoveryAsync(string primaryRegion, List<string> secondaryRegions);

        /// <summary>
        /// Gets disaster recovery metrics and statistics.
        /// </summary>
        Task<DisasterRecoveryMetrics> GetMetricsAsync();

        /// <summary>
        /// Cancels a backup or recovery operation.
        /// </summary>
        Task<bool> CancelOperationAsync(string operationId);
    }

    /// <summary>
    /// Backup operation details.
    /// </summary>
    public class BackupOperation
    {
        public string BackupId { get; set; }
        public string BackupName { get; set; }
        public BackupType BackupType { get; set; }
        public BackupStatus Status { get; set; }
        public List<string> Targets { get; set; } = new();
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public long SizeBytes { get; set; }
        public long FilesBackedUp { get; set; }
        public string? DestinationId { get; set; }
        public double ProgressPercent { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ChecksumSha256 { get; set; }
        public int RetentionDays { get; set; }
    }

    /// <summary>
    /// Recovery operation details.
    /// </summary>
    public class RecoveryOperation
    {
        public string RecoveryId { get; set; }
        public string BackupId { get; set; }
        public RecoveryType RecoveryType { get; set; }
        public RecoveryStatus Status { get; set; }
        public List<string> TargetResources { get; set; } = new();
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public double ProgressPercent { get; set; }
        public long FilesRecovered { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? PointInTimeRecovery { get; set; }
        public TimeSpan? ActualRecoveryTimeObjective { get; set; }
    }

    /// <summary>
    /// Disaster recovery metrics.
    /// </summary>
    public class DisasterRecoveryMetrics
    {
        public int TotalBackups { get; set; }
        public int SuccessfulBackups { get; set; }
        public int FailedBackups { get; set; }
        public long TotalBackupSizeBytes { get; set; }
        public int TotalRecoveries { get; set; }
        public int SuccessfulRecoveries { get; set; }
        public int FailedRecoveries { get; set; }
        public double AverageBackupTimeMinutes { get; set; }
        public double AverageRecoveryTimeMinutes { get; set; }
        public List<ResourceRpoStatus> ResourceRpoStatuses { get; set; } = new();
        public DateTime CollectedAt { get; set; }
    }

    /// <summary>
    /// RPO status for a resource.
    /// </summary>
    public class ResourceRpoStatus
    {
        public string ResourceId { get; set; }
        public int ConfiguredRpoMinutes { get; set; }
        public int ActualRpoMinutes { get; set; }
        public bool IsCompliant { get; set; }
        public DateTime LastBackupAt { get; set; }
    }

    /// <summary>
    /// Backup type enumeration.
    /// </summary>
    public enum BackupType
    {
        Full,
        Incremental,
        Differential,
        Snapshot,
        Mirror
    }

    /// <summary>
    /// Backup status enumeration.
    /// </summary>
    public enum BackupStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Cancelled,
        VerifyingIntegrity
    }

    /// <summary>
    /// Recovery type enumeration.
    /// </summary>
    public enum RecoveryType
    {
        Full,
        Partial,
        PointInTime,
        RollForward,
        Failover
    }

    /// <summary>
    /// Recovery status enumeration.
    /// </summary>
    public enum RecoveryStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Cancelled,
        VerifyingRecovery
    }
}
