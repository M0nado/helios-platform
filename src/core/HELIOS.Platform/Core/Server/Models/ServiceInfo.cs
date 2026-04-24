using System;
using System.Collections.Generic;

namespace HELIOS.Platform.Core.Server.Models
{
    /// <summary>
    /// Represents information about a Windows service or managed process.
    /// </summary>
    public class ServiceInfo
    {
        /// <summary>
        /// Unique identifier for the service.
        /// </summary>
        public string ServiceId { get; set; } = string.Empty;

        /// <summary>
        /// Display name of the service.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Current status of the service.
        /// </summary>
        public ServiceStatus Status { get; set; } = ServiceStatus.Stopped;

        /// <summary>
        /// Current running state of the service.
        /// </summary>
        public ServiceRunningState RunningState { get; set; } = ServiceRunningState.Stopped;

        /// <summary>
        /// Startup type of the service.
        /// </summary>
        public ServiceStartupType StartupType { get; set; } = ServiceStartupType.Manual;

        /// <summary>
        /// Process ID if service is running, null otherwise.
        /// </summary>
        public int? ProcessId { get; set; }

        /// <summary>
        /// Service uptime duration.
        /// </summary>
        public TimeSpan Uptime { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// When the service last started.
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Memory usage in bytes.
        /// </summary>
        public long MemoryUsage { get; set; }

        /// <summary>
        /// CPU usage percentage (0-100).
        /// </summary>
        public double CpuUsage { get; set; }

        /// <summary>
        /// Disk I/O in bytes per second.
        /// </summary>
        public double DiskIoRate { get; set; }

        /// <summary>
        /// List of services this service depends on.
        /// </summary>
        public List<string> Dependencies { get; set; } = new();

        /// <summary>
        /// List of services that depend on this service.
        /// </summary>
        public List<string> Dependents { get; set; } = new();

        /// <summary>
        /// Services in the same cluster for redundancy.
        /// </summary>
        public List<string> ClusterMembers { get; set; } = new();

        /// <summary>
        /// Whether auto-restart is enabled on failure.
        /// </summary>
        public bool AutoRestartEnabled { get; set; } = true;

        /// <summary>
        /// Maximum restart attempts before giving up.
        /// </summary>
        public int MaxRestartAttempts { get; set; } = 3;

        /// <summary>
        /// Current restart attempt count.
        /// </summary>
        public int CurrentRestartAttempts { get; set; }

        /// <summary>
        /// Health check status.
        /// </summary>
        public HealthStatus HealthStatus { get; set; } = HealthStatus.Unknown;

        /// <summary>
        /// Last health check time.
        /// </summary>
        public DateTime? LastHealthCheckTime { get; set; }

        /// <summary>
        /// Number of restarts in the last hour.
        /// </summary>
        public int RestartsLastHour { get; set; }

        /// <summary>
        /// Total number of restarts since service creation.
        /// </summary>
        public int TotalRestarts { get; set; }

        /// <summary>
        /// Description of the service.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Service type (Windows Service or Custom Process).
        /// </summary>
        public ServiceType ServiceType { get; set; } = ServiceType.WindowsService;

        /// <summary>
        /// When the service was created/registered.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last update time.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Service status enumeration.
    /// </summary>
    public enum ServiceStatus
    {
        Stopped = 0,
        Running = 1,
        Paused = 2,
        Unknown = 3
    }

    /// <summary>
    /// Service running state enumeration.
    /// </summary>
    public enum ServiceRunningState
    {
        Stopped = 1,
        StartPending = 2,
        StopPending = 3,
        Running = 4,
        ContinuePending = 5,
        PausePending = 6,
        Paused = 7
    }

    /// <summary>
    /// Service startup type enumeration.
    /// </summary>
    public enum ServiceStartupType
    {
        Automatic = 2,
        Manual = 3,
        Disabled = 4,
        AutomaticDelayedStart = 5
    }

    /// <summary>
    /// Health status enumeration.
    /// </summary>
    public enum HealthStatus
    {
        Healthy = 0,
        Unhealthy = 1,
        Warning = 2,
        Unknown = 3
    }

    /// <summary>
    /// Service type enumeration.
    /// </summary>
    public enum ServiceType
    {
        WindowsService = 0,
        CustomProcess = 1,
        DockerContainer = 2,
        ManagedApplication = 3
    }
}
