using System;
using System.Collections.Generic;

namespace HELIOS.Platform.Phase10.AIOrchestration.Models
{
    /// <summary>
    /// Represents metadata and state information for a system tool
    /// </summary>
    public class ToolInfo
    {
        public string ToolId { get; set; }
        public string ToolName { get; set; }
        public string Category { get; set; }
        public string Version { get; set; }
        public ToolStatus Status { get; set; }
        public DateTime LastHealthCheck { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ToolHealthMetrics HealthMetrics { get; set; }
        public ToolPerformanceMetrics PerformanceMetrics { get; set; }
        public List<string> Dependencies { get; set; }
        public ToolResourceAllocation CurrentAllocation { get; set; }
        public Dictionary<string, object> Configuration { get; set; }

        public ToolInfo()
        {
            ToolId = Guid.NewGuid().ToString();
            Dependencies = new List<string>();
            Configuration = new Dictionary<string, object>();
            HealthMetrics = new ToolHealthMetrics();
            PerformanceMetrics = new ToolPerformanceMetrics();
            CurrentAllocation = new ToolResourceAllocation();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public enum ToolStatus
    {
        Initializing,
        Running,
        Idle,
        Degraded,
        Failed,
        Restarting,
        Stopped
    }

    /// <summary>
    /// Health metrics for a tool
    /// </summary>
    public class ToolHealthMetrics
    {
        public int HealthScore { get; set; } // 0-100
        public int CrashCount { get; set; }
        public int HangCount { get; set; }
        public DateTime LastCrash { get; set; }
        public DateTime LastHang { get; set; }
        public List<string> RecentErrors { get; set; }
        public double Uptime { get; set; } // in hours
        public bool IsUnresponsive { get; set; }

        public ToolHealthMetrics()
        {
            HealthScore = 100;
            RecentErrors = new List<string>();
            Uptime = 0;
        }
    }

    /// <summary>
    /// Performance metrics for a tool
    /// </summary>
    public class ToolPerformanceMetrics
    {
        public double AverageCpuUsage { get; set; }
        public double PeakCpuUsage { get; set; }
        public long AverageMemoryUsage { get; set; }
        public long PeakMemoryUsage { get; set; }
        public double AverageDiskIO { get; set; }
        public double AverageGpuUsage { get; set; }
        public double ResponseTimeMs { get; set; }
        public double ThroughputOpsPerSec { get; set; }
        public double LatencyP50 { get; set; }
        public double LatencyP99 { get; set; }
        public int OperationCount { get; set; }

        public ToolPerformanceMetrics()
        {
            OperationCount = 0;
        }
    }

    /// <summary>
    /// Resource allocation for a tool
    /// </summary>
    public class ToolResourceAllocation
    {
        public int MaxCpuPercent { get; set; }
        public long MaxMemoryMB { get; set; }
        public int MaxDiskIOOps { get; set; }
        public int MaxGpuPercent { get; set; }
        public int Priority { get; set; } // 0-10, higher = more critical

        public ToolResourceAllocation()
        {
            MaxCpuPercent = 50;
            MaxMemoryMB = 512;
            MaxDiskIOOps = 1000;
            MaxGpuPercent = 50;
            Priority = 5;
        }
    }

    /// <summary>
    /// Tool configuration for different profiles
    /// </summary>
    public class ToolProfileConfig
    {
        public string ProfileName { get; set; }
        public ToolResourceAllocation ResourceAllocation { get; set; }
        public Dictionary<string, object> Settings { get; set; }
        public Dictionary<string, int> Thresholds { get; set; }
        public bool Enabled { get; set; }

        public ToolProfileConfig()
        {
            ResourceAllocation = new ToolResourceAllocation();
            Settings = new Dictionary<string, object>();
            Thresholds = new Dictionary<string, int>();
            Enabled = true;
        }
    }
}
