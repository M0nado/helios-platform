using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Phase10.BootEnvironment
{
    /// <summary>
    /// Boot environment information and diagnostics data.
    /// </summary>
    public class BootEnvironmentInfo
    {
        public bool IsUEFISupported { get; set; }
        public bool IsBIOSSupported { get; set; }
        public bool IsUEFIActive { get; set; }
        public string FirmwareType { get; set; }
        public long TotalMemoryMB { get; set; }
        public int ProcessorCount { get; set; }
        public bool IsSecureBootEnabled { get; set; }
        public List<string> AvailableDiskDrives { get; set; } = new();
        public Dictionary<string, string> BootMenuOptions { get; set; } = new();
        public DateTime LastBootTime { get; set; }
        public long UptimeSeconds { get; set; }
    }

    /// <summary>
    /// USB device information and health status.
    /// </summary>
    public class USBDeviceInfo
    {
        public string DeviceId { get; set; }
        public string FriendlyName { get; set; }
        public long CapacityBytes { get; set; }
        public long UsedBytes { get; set; }
        public string FileSystem { get; set; }
        public bool IsHealthy { get; set; }
        public int ErrorCount { get; set; }
        public DateTime LastHealthCheck { get; set; }
        public float HealthPercentage { get; set; }
    }

    /// <summary>
    /// Boot configuration and menu settings.
    /// </summary>
    public class BootConfiguration
    {
        public string BootLoaderPath { get; set; }
        public int DefaultBootOption { get; set; }
        public int BootTimeoutSeconds { get; set; }
        public bool EnableGraphicalMenu { get; set; }
        public bool EnableNetworkBoot { get; set; }
        public List<BootMenuEntry> MenuEntries { get; set; } = new();
    }

    /// <summary>
    /// Individual boot menu entry.
    /// </summary>
    public class BootMenuEntry
    {
        public int OrderIndex { get; set; }
        public string DisplayName { get; set; }
        public string LoaderPath { get; set; }
        public string Description { get; set; }
        public bool IsDefault { get; set; }
    }

    /// <summary>
    /// Recovery partition information.
    /// </summary>
    public class RecoveryPartitionInfo
    {
        public string PartitionId { get; set; }
        public long SizeBytes { get; set; }
        public long UsedBytes { get; set; }
        public string FileSystem { get; set; }
        public bool ContainsWinRE { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsHealthy { get; set; }
    }

    /// <summary>
    /// Boot diagnostics result.
    /// </summary>
    public class BootDiagnosticsResult
    {
        public bool OverallHealthy { get; set; }
        public List<string> DiagnosticMessages { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public DateTime DiagnosticsTimestamp { get; set; }
        public long ExecutionTimeMs { get; set; }
    }

    /// <summary>
    /// Integration interface for USB Boot Environment services.
    /// Coordinates all boot environment operations including WinPE creation,
    /// ISO building, USB deployment, boot menus, diagnostics, and health monitoring.
    /// </summary>
    public interface IBootEnvironmentService
    {
        // USB Bootstrap Engine operations
        Task<bool> CreateWinPEEnvironmentAsync(string outputPath, bool includeUEFI = true, bool includeLegacy = true);
        Task<bool> ConfigureBootEnvironmentAsync(string peRoot, BootConfiguration config);
        Task<bool> ValidateBootEnvironmentAsync(string peRoot);

        // ISO Image Builder operations
        Task<string> BuildISOImageAsync(string peRoot, string outputPath, string isoName, bool optimizeSize = true);
        Task<bool> VerifyISOIntegrityAsync(string isoPath);
        Task<long> GetISOSizeAsync(string isoPath);

        // USB Flasher operations
        Task<bool> WriteISOToUSBAsync(string isoPath, string usbDeviceId, bool verifyWrite = true);
        Task<bool> VerifyUSBBootabilityAsync(string usbDeviceId);
        Task<bool> SafeEjectUSBAsync(string usbDeviceId);

        // Boot Menu Manager operations
        Task<BootConfiguration> CreateBootMenuAsync(List<string> menuItems);
        Task<bool> UpdateBootMenuAsync(string bootConfigPath, BootConfiguration config);
        Task<bool> SetDefaultBootOptionAsync(string bootConfigPath, int optionIndex);

        // Pre-Boot Environment operations
        Task<bool> LoadPEDriversAsync(string peRoot, List<string> driverPaths);
        Task<bool> SetupPENetworkAsync(string peRoot, string ipAddress = null);
        Task<bool> InitializePEStorageAsync(string peRoot);

        // Boot Diagnostics operations
        Task<BootEnvironmentInfo> GetBootEnvironmentInfoAsync();
        Task<BootDiagnosticsResult> RunBootDiagnosticsAsync();
        Task<bool> ValidateBootFirmwareAsync();
        Task<bool> CheckCPUSupportAsync();

        // Recovery Partition Manager operations
        Task<bool> CreateRecoveryPartitionAsync(string targetDisk, long sizeBytes);
        Task<bool> BackupWinREAsync(string recoveryPartition, string backupPath);
        Task<bool> RestoreWinREAsync(string recoveryPartition, string backupPath);
        Task<RecoveryPartitionInfo> GetRecoveryPartitionInfoAsync(string partitionId);

        // USB Health Monitor operations
        Task<USBDeviceInfo> GetUSBHealthAsync(string deviceId);
        Task<bool> MonitorUSBHealthAsync(string deviceId, TimeSpan checkInterval);
        Task<bool> StopUSBMonitorAsync(string deviceId);
        Task<List<USBDeviceInfo>> GetAllUSBDevicesAsync();
    }
}
