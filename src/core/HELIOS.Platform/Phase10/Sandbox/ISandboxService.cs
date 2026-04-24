using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Phase10.Sandbox
{
    /// <summary>
    /// Core interface for sandbox environment operations
    /// </summary>
    public interface ISandboxService
    {
        Task<bool> InitializeAsync(CancellationToken cancellationToken = default);
        Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
        Task ShutdownAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Sandbox environment setup and configuration
    /// </summary>
    public interface ISandboxEnvironmentSetup : ISandboxService
    {
        Task<SandboxConfiguration> GetCurrentConfigurationAsync(CancellationToken cancellationToken = default);
        Task<bool> SetupSandboxPartitionAsync(string partitionPath, long sizeGb, CancellationToken cancellationToken = default);
        Task<bool> ConfigureSharedFolderAsync(string hostPath, string sandboxPath, CancellationToken cancellationToken = default);
        Task<bool> SetResourceLimitsAsync(SandboxResourceLimits limits, CancellationToken cancellationToken = default);
        Task<bool> ConfigureNetworkIsolationAsync(NetworkIsolationPolicy policy, CancellationToken cancellationToken = default);
        Task<bool> EnableGpuPassThroughAsync(CancellationToken cancellationToken = default);
        Task<bool> CreateSnapshotCapabilityAsync(CancellationToken cancellationToken = default);
        Task<SandboxEnvironmentInfo> GetEnvironmentInfoAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Sandbox launcher and lifecycle management
    /// </summary>
    public interface ISandboxLauncher : ISandboxService
    {
        Task<SandboxInstance> LaunchSandboxAsync(SandboxLaunchOptions options, CancellationToken cancellationToken = default);
        Task<bool> MountSharedFolderAsync(SandboxInstance sandbox, string hostPath, string sandboxPath, CancellationToken cancellationToken = default);
        Task<bool> PassFileForTestingAsync(SandboxInstance sandbox, string filePath, CancellationToken cancellationToken = default);
        Task<SandboxExecutionResult> ExecuteInSandboxAsync(SandboxInstance sandbox, string command, int timeoutSeconds, CancellationToken cancellationToken = default);
        Task<bool> VerifyIsolationAsync(SandboxInstance sandbox, CancellationToken cancellationToken = default);
        Task<SandboxLogs> GetSandboxLogsAsync(SandboxInstance sandbox, CancellationToken cancellationToken = default);
        Task<bool> TerminateSandboxAsync(SandboxInstance sandbox, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// File transfer and management within sandbox
    /// </summary>
    public interface ISandboxFileTransfer : ISandboxService
    {
        Task<bool> TransferFileToSandboxAsync(SandboxInstance sandbox, string sourceFile, string destinationPath, CancellationToken cancellationToken = default);
        Task<bool> TransferFileFromSandboxAsync(SandboxInstance sandbox, string sandboxPath, string destinationPath, CancellationToken cancellationToken = default);
        Task<SandboxFileActivity> MonitorFileInSandboxAsync(SandboxInstance sandbox, string filePath, CancellationToken cancellationToken = default);
        Task<SandboxActivityReport> CaptureActivityAsync(SandboxInstance sandbox, CancellationToken cancellationToken = default);
        Task<ContaminationCheckResult> VerifyNoContaminationAsync(SandboxInstance sandbox, CancellationToken cancellationToken = default);
        Task<FileTransferLog> GetTransferLogAsync(SandboxInstance sandbox, CancellationToken cancellationToken = default);
        Task<bool> CleanupTransferAsync(SandboxInstance sandbox, string path, CancellationToken cancellationToken = default);
        Task<bool> ArchiveAnalysisResultsAsync(SandboxInstance sandbox, string archivePath, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Sandbox activity monitoring and threat detection
    /// </summary>
    public interface ISandboxMonitor : ISandboxService
    {
        Task StartMonitoringAsync(SandboxInstance sandbox, CancellationToken cancellationToken = default);
        Task StopMonitoringAsync(SandboxInstance sandbox, CancellationToken cancellationToken = default);
        Task<IEnumerable<FileOperation>> GetFileOperationsAsync(SandboxInstance sandbox, CancellationToken cancellationToken = default);
        Task<IEnumerable<RegistryOperation>> GetRegistryAccessAsync(SandboxInstance sandbox, CancellationToken cancellationToken = default);
        Task<IEnumerable<NetworkOperation>> GetNetworkAccessAsync(SandboxInstance sandbox, CancellationToken cancellationToken = default);
        Task<IEnumerable<ProcessOperation>> GetProcessActivityAsync(SandboxInstance sandbox, CancellationToken cancellationToken = default);
        Task<ActivityReport> GenerateActivityReportAsync(SandboxInstance sandbox, CancellationToken cancellationToken = default);
        Task<ThreatDetectionResult> DetectMalwareBehaviorAsync(SandboxInstance sandbox, CancellationToken cancellationToken = default);
        Task<bool> AutoTerminateOnDangerAsync(SandboxInstance sandbox, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Sandbox snapshot management
    /// </summary>
    public interface ISandboxSnapshotManager : ISandboxService
    {
        Task<SandboxSnapshot> CreateSnapshotAsync(SandboxInstance sandbox, string snapshotName, CancellationToken cancellationToken = default);
        Task<bool> RestoreFromSnapshotAsync(SandboxInstance sandbox, SandboxSnapshot snapshot, CancellationToken cancellationToken = default);
        Task<IEnumerable<SandboxSnapshot>> GetSnapshotsAsync(SandboxInstance sandbox, CancellationToken cancellationToken = default);
        Task<bool> ScheduleSnapshotAsync(SandboxInstance sandbox, TimeSpan interval, CancellationToken cancellationToken = default);
        Task<bool> CompressSnapshotAsync(SandboxSnapshot snapshot, CancellationToken cancellationToken = default);
        Task<bool> RapidRollbackAsync(SandboxInstance sandbox, CancellationToken cancellationToken = default);
        Task<bool> DeleteSnapshotAsync(SandboxSnapshot snapshot, CancellationToken cancellationToken = default);
        Task<SnapshotManagementReport> GetManagementReportAsync(SandboxInstance sandbox, CancellationToken cancellationToken = default);
    }

    // ========== Data Transfer Objects ==========

    public class SandboxConfiguration
    {
        public string SandboxType { get; set; }
        public string StoragePath { get; set; }
        public SandboxResourceLimits ResourceLimits { get; set; }
        public NetworkIsolationPolicy NetworkPolicy { get; set; }
        public bool GpuEnabled { get; set; }
        public bool SnapshotCapable { get; set; }
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, string> CustomSettings { get; set; }
    }

    public class SandboxResourceLimits
    {
        public int CpuCores { get; set; }
        public long RamMb { get; set; }
        public long DiskGb { get; set; }
        public long NetworkBandwidthMbps { get; set; }
    }

    public enum NetworkIsolationPolicy
    {
        Full,
        Restricted,
        LocalhostOnly,
        CustomRules
    }

    public class SandboxEnvironmentInfo
    {
        public bool IsAvailable { get; set; }
        public string SandboxType { get; set; }
        public bool PartitionAvailable { get; set; }
        public long PartitionSizeGb { get; set; }
        public bool GpuSupported { get; set; }
        public int MaxConcurrentSandboxes { get; set; }
    }

    public class SandboxInstance
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? ProcessId { get; set; }
        public SandboxStatus Status { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    public enum SandboxStatus
    {
        Created,
        Running,
        Suspended,
        Stopped,
        Error
    }

    public class SandboxLaunchOptions
    {
        public string SandboxName { get; set; }
        public SandboxResourceLimits ResourceLimits { get; set; }
        public NetworkIsolationPolicy NetworkPolicy { get; set; }
        public Dictionary<string, string> SharedFolders { get; set; }
        public bool EnableGpu { get; set; }
        public int TimeoutSeconds { get; set; }
    }

    public class SandboxExecutionResult
    {
        public bool Success { get; set; }
        public string Output { get; set; }
        public string Error { get; set; }
        public int ExitCode { get; set; }
        public DateTime ExecutedAt { get; set; }
        public long ExecutionTimeMs { get; set; }
    }

    public class SandboxLogs
    {
        public string SystemLog { get; set; }
        public string ApplicationLog { get; set; }
        public string SecurityLog { get; set; }
        public DateTime RetrievedAt { get; set; }
    }

    public class SandboxFileActivity
    {
        public string FilePath { get; set; }
        public List<FileOperation> Operations { get; set; }
        public bool Modified { get; set; }
        public DateTime MonitoredAt { get; set; }
    }

    public class FileOperation
    {
        public string OperationType { get; set; }
        public string TargetPath { get; set; }
        public DateTime Timestamp { get; set; }
        public string ProcessName { get; set; }
    }

    public class SandboxActivityReport
    {
        public int FileOperations { get; set; }
        public int RegistryAccesses { get; set; }
        public int NetworkOperations { get; set; }
        public int ProcessCreations { get; set; }
        public List<FileOperation> FileOps { get; set; }
        public List<RegistryOperation> RegistryOps { get; set; }
        public List<NetworkOperation> NetworkOps { get; set; }
        public List<ProcessOperation> ProcessOps { get; set; }
    }

    public class RegistryOperation
    {
        public string KeyPath { get; set; }
        public string ValueName { get; set; }
        public string OperationType { get; set; }
        public DateTime Timestamp { get; set; }
        public string ProcessName { get; set; }
    }

    public class NetworkOperation
    {
        public string Protocol { get; set; }
        public string RemoteAddress { get; set; }
        public int RemotePort { get; set; }
        public DateTime Timestamp { get; set; }
        public string ProcessName { get; set; }
    }

    public class ProcessOperation
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }
        public string CommandLine { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ContaminationCheckResult
    {
        public bool IsClean { get; set; }
        public List<string> SuspiciousItems { get; set; }
        public List<string> ModifiedSystemFiles { get; set; }
        public DateTime CheckedAt { get; set; }
    }

    public class FileTransferLog
    {
        public List<TransferRecord> Transfers { get; set; }
        public long TotalBytesTransferred { get; set; }
        public DateTime LogCreatedAt { get; set; }
    }

    public class TransferRecord
    {
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }
        public long Size { get; set; }
        public string Hash { get; set; }
        public DateTime TransferredAt { get; set; }
    }

    public class ActivityReport
    {
        public int TotalOperations { get; set; }
        public int FileOperations { get; set; }
        public int RegistryOperations { get; set; }
        public int NetworkOperations { get; set; }
        public int ProcessOperations { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class ThreatDetectionResult
    {
        public bool ThreatDetected { get; set; }
        public string ThreatLevel { get; set; }
        public List<string> ThreatIndicators { get; set; }
        public List<string> SuspiciousBehaviors { get; set; }
        public DateTime AnalyzedAt { get; set; }
    }

    public class SandboxSnapshot
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public long SizeMb { get; set; }
        public bool Compressed { get; set; }
        public string Path { get; set; }
        public string Hash { get; set; }
    }

    public class SnapshotManagementReport
    {
        public int TotalSnapshots { get; set; }
        public long TotalStorageMb { get; set; }
        public List<SandboxSnapshot> Snapshots { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
