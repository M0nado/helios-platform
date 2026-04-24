using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Phase10.Drivers
{
    /// <summary>
    /// Represents driver information and metadata.
    /// </summary>
    public class DriverInfo
    {
        public string DriverId { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string ChipsetInfo { get; set; }
        public string DeviceName { get; set; }
        public string DeviceId { get; set; }
        public string DriverType { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsStable { get; set; }
        public string Manufacturer { get; set; }
        public DateTime InstalledDate { get; set; }
    }

    /// <summary>
    /// Represents detected hardware device.
    /// </summary>
    public class DetectedDevice
    {
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string DeviceType { get; set; }
        public string Status { get; set; }
        public string Manufacturer { get; set; }
        public string PnpDeviceId { get; set; }
        public string Description { get; set; }
        public bool RequiresDriver { get; set; }
        public string CurrentDriverVersion { get; set; }
    }

    /// <summary>
    /// Represents driver download progress.
    /// </summary>
    public class DownloadProgress
    {
        public string DriverName { get; set; }
        public long BytesDownloaded { get; set; }
        public long TotalBytes { get; set; }
        public int Percentage { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan EstimatedTimeRemaining { get; set; }
        public bool IsComplete { get; set; }
    }

    /// <summary>
    /// Represents driver installation result.
    /// </summary>
    public class InstallationResult
    {
        public bool Success { get; set; }
        public string DriverName { get; set; }
        public string Version { get; set; }
        public string Message { get; set; }
        public DateTime InstallDate { get; set; }
        public string LogPath { get; set; }
        public Exception Error { get; set; }
    }

    /// <summary>
    /// Represents driver health status.
    /// </summary>
    public class DriverHealthStatus
    {
        public string DriverId { get; set; }
        public string DriverName { get; set; }
        public bool IsHealthy { get; set; }
        public int ErrorCount { get; set; }
        public int CrashCount { get; set; }
        public DateTime LastCheckTime { get; set; }
        public string HealthReason { get; set; }
        public List<string> RecentErrors { get; set; }
    }

    /// <summary>
    /// Comprehensive driver management service interface.
    /// </summary>
    public interface IDriverService
    {
        #region Detection
        /// <summary>
        /// Detect all hardware devices requiring drivers.
        /// </summary>
        Task<List<DetectedDevice>> DetectHardwareAsync();

        /// <summary>
        /// Detect specific device type.
        /// </summary>
        Task<List<DetectedDevice>> DetectDeviceTypeAsync(string deviceType);

        /// <summary>
        /// Check if GPU is installed.
        /// </summary>
        Task<bool> HasGpuAsync();

        /// <summary>
        /// Check for biometric devices.
        /// </summary>
        Task<List<DetectedDevice>> DetectBiometricDevicesAsync();
        #endregion

        #region Repository
        /// <summary>
        /// Get driver from local repository.
        /// </summary>
        Task<DriverInfo> GetDriverAsync(string driverId);

        /// <summary>
        /// Get all cached drivers.
        /// </summary>
        Task<List<DriverInfo>> GetAllCachedDriversAsync();

        /// <summary>
        /// Store driver in repository.
        /// </summary>
        Task StoreDriverAsync(DriverInfo driver, string filePath);

        /// <summary>
        /// Remove driver from repository.
        /// </summary>
        Task RemoveDriverAsync(string driverId);

        /// <summary>
        /// Get drivers by type.
        /// </summary>
        Task<List<DriverInfo>> GetDriversByTypeAsync(string driverType);
        #endregion

        #region Download
        /// <summary>
        /// Download missing drivers.
        /// </summary>
        Task<List<DriverInfo>> DownloadMissingDriversAsync();

        /// <summary>
        /// Download specific driver.
        /// </summary>
        Task<DriverInfo> DownloadDriverAsync(string manufacturerId, string chipsetId);

        /// <summary>
        /// Download with progress callback.
        /// </summary>
        Task DownloadWithProgressAsync(string driverId, Action<DownloadProgress> progressCallback);

        /// <summary>
        /// Resume interrupted download.
        /// </summary>
        Task<DriverInfo> ResumeDownloadAsync(string driverId);

        /// <summary>
        /// Verify driver checksum.
        /// </summary>
        Task<bool> VerifyChecksumAsync(string filePath, string expectedChecksum);
        #endregion

        #region Installation
        /// <summary>
        /// Install downloaded drivers.
        /// </summary>
        Task<List<InstallationResult>> InstallAllDriversAsync();

        /// <summary>
        /// Install specific driver.
        /// </summary>
        Task<InstallationResult> InstallDriverAsync(string driverId);

        /// <summary>
        /// Create system restore point before installation.
        /// </summary>
        Task<bool> CreateRestorePointAsync(string description);

        /// <summary>
        /// Get installation history.
        /// </summary>
        Task<List<InstallationResult>> GetInstallationHistoryAsync();

        /// <summary>
        /// Verify driver installation.
        /// </summary>
        Task<bool> VerifyInstallationAsync(string driverId);
        #endregion

        #region Updates
        /// <summary>
        /// Check for driver updates.
        /// </summary>
        Task<List<DriverInfo>> CheckForUpdatesAsync();

        /// <summary>
        /// Auto-update all drivers.
        /// </summary>
        Task<List<InstallationResult>> AutoUpdateAsync();

        /// <summary>
        /// Schedule periodic driver checks.
        /// </summary>
        Task ScheduleUpdateCheckAsync(TimeSpan interval);

        /// <summary>
        /// Get available driver updates.
        /// </summary>
        Task<List<DriverInfo>> GetAvailableUpdatesAsync();

        /// <summary>
        /// Update single driver.
        /// </summary>
        Task<InstallationResult> UpdateDriverAsync(string driverId);
        #endregion

        #region Rollback
        /// <summary>
        /// Rollback to previous driver version.
        /// </summary>
        Task<bool> RollbackDriverAsync(string driverId);

        /// <summary>
        /// Get driver backup information.
        /// </summary>
        Task<List<DriverInfo>> GetAvailableBackupsAsync(string driverId);

        /// <summary>
        /// Detect problematic drivers.
        /// </summary>
        Task<List<string>> DetectProblematicDriversAsync();

        /// <summary>
        /// Automatic rollback if needed.
        /// </summary>
        Task<bool> AutoRollbackAsync(string driverId);

        /// <summary>
        /// Restore from Windows restore point.
        /// </summary>
        Task<bool> RestoreFromRestorePointAsync(string restorePointId);
        #endregion

        #region Health
        /// <summary>
        /// Monitor driver health and stability.
        /// </summary>
        Task<DriverHealthStatus> CheckDriverHealthAsync(string driverId);

        /// <summary>
        /// Get overall system driver health.
        /// </summary>
        Task<List<DriverHealthStatus>> GetSystemHealthAsync();

        /// <summary>
        /// Detect driver crashes.
        /// </summary>
        Task<List<string>> DetectDriverCrashesAsync();

        /// <summary>
        /// Generate stability report.
        /// </summary>
        Task<string> GenerateStabilityReportAsync();

        /// <summary>
        /// Get driver events log.
        /// </summary>
        Task<List<string>> GetDriverEventsAsync(int maxEvents = 100);
        #endregion

        #region Lifecycle
        /// <summary>
        /// Initialize driver service.
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Shutdown driver service.
        /// </summary>
        Task ShutdownAsync();

        /// <summary>
        /// Clear all cached drivers.
        /// </summary>
        Task ClearCacheAsync();

        /// <summary>
        /// Get service status.
        /// </summary>
        Task<bool> GetStatusAsync();
        #endregion
    }
}
