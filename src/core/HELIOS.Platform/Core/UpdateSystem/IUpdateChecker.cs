using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.UpdateSystem
{
    /// <summary>
    /// Interface for checking and managing system updates
    /// </summary>
    public interface IUpdateChecker
    {
        /// <summary>
        /// Check for available updates
        /// </summary>
        Task<UpdateInfo> CheckForUpdatesAsync();

        /// <summary>
        /// Download update asynchronously
        /// </summary>
        Task<bool> DownloadUpdateAsync(string version, IProgress<DownloadProgress> progress = null);

        /// <summary>
        /// Apply downloaded update
        /// </summary>
        Task<bool> ApplyUpdateAsync(string version);

        /// <summary>
        /// Rollback to previous version
        /// </summary>
        Task<bool> RollbackAsync();

        /// <summary>
        /// Get update history
        /// </summary>
        Task<IEnumerable<UpdateRecord>> GetUpdateHistoryAsync();

        /// <summary>
        /// Get current update status
        /// </summary>
        Task<UpdateStatus> GetStatusAsync();

        /// <summary>
        /// Check version compatibility
        /// </summary>
        Task<bool> CheckCompatibilityAsync(string targetVersion);

        /// <summary>
        /// Perform delta update (only changed files)
        /// </summary>
        Task<bool> PerformDeltaUpdateAsync(string version, IProgress<DownloadProgress> progress = null);

        /// <summary>
        /// Schedule update for later
        /// </summary>
        Task ScheduleUpdateAsync(DateTime scheduledTime, string version);

        /// <summary>
        /// Cancel scheduled update
        /// </summary>
        Task CancelScheduledUpdateAsync();

        /// <summary>
        /// Check for offline updates
        /// </summary>
        Task<bool> CheckOfflineUpdateAsync(string updatePath);
    }

    /// <summary>
    /// Information about available updates
    /// </summary>
    public class UpdateInfo
    {
        public string CurrentVersion { get; set; }
        public string LatestVersion { get; set; }
        public string ReleaseNotes { get; set; }
        public DateTime ReleaseDate { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsCritical { get; set; }
        public bool IsMandatory { get; set; }
        public long SizeInBytes { get; set; }
        public string DownloadUrl { get; set; }
        public bool SupportsStaged { get; set; }
        public bool SupportsDelta { get; set; }
    }

    /// <summary>
    /// Download progress information
    /// </summary>
    public class DownloadProgress
    {
        public long BytesDownloaded { get; set; }
        public long TotalBytes { get; set; }
        public int ProgressPercentage { get; set; }
        public double DownloadSpeedMbps { get; set; }
        public TimeSpan EstimatedTimeRemaining { get; set; }
    }

    /// <summary>
    /// Update status information
    /// </summary>
    public class UpdateStatus
    {
        public UpdatePhase Phase { get; set; }
        public int ProgressPercentage { get; set; }
        public string CurrentVersion { get; set; }
        public string UpdateVersion { get; set; }
        public DateTime LastCheckTime { get; set; }
        public DateTime? ScheduledUpdateTime { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Update record for history tracking
    /// </summary>
    public class UpdateRecord
    {
        public string FromVersion { get; set; }
        public string ToVersion { get; set; }
        public DateTime UpdateTime { get; set; }
        public bool Success { get; set; }
        public string Notes { get; set; }
        public long SizeInBytes { get; set; }
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Update phases
    /// </summary>
    public enum UpdatePhase
    {
        Idle,
        CheckingForUpdates,
        UpdateAvailable,
        Downloading,
        Downloaded,
        Preparing,
        Installing,
        Finalizing,
        Completed,
        Failed,
        Scheduled,
        Staged,
        RollingBack
    }
}
