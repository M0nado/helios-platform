using System;
using System.Diagnostics;

namespace HELIOS.Platform.Presentation.Studio.Models
{
    /// <summary>
    /// Real-time system metrics for dashboard display
    /// </summary>
    public class SystemMetrics
    {
        public DateTime Timestamp { get; set; }
        public double CpuUsagePercent { get; set; }
        public double MemoryUsagePercent { get; set; }
        public long MemoryUsedMB { get; set; }
        public long MemoryTotalMB { get; set; }
        public double DiskUsagePercent { get; set; }
        public long DiskUsedGB { get; set; }
        public long DiskTotalGB { get; set; }
        public double NetworkBytesPerSecond { get; set; }
        public double GpuUsagePercent { get; set; }
        public double GpuMemoryUsagePercent { get; set; }
        public int ProcessCount { get; set; }
        public int ThreadCount { get; set; }
    }

    /// <summary>
    /// Dashboard alert information
    /// </summary>
    public class DashboardAlert
    {
        public string Id { get; set; }
        public AlertSeverity Severity { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public bool IsResolved => ResolvedAt.HasValue;
    }

    public enum AlertSeverity
    {
        Info = 0,
        Warning = 1,
        Critical = 2,
        Error = 3
    }

    /// <summary>
    /// User information for admin dashboard
    /// </summary>
    public class DashboardUser
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string[] Roles { get; set; }
        public DateTime LastLoginAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Global settings configuration
    /// </summary>
    public class DashboardSettings
    {
        public int RefreshIntervalSeconds { get; set; } = 5;
        public bool EnableNotifications { get; set; } = true;
        public bool EnableDarkMode { get; set; } = false;
        public int CpuAlertThreshold { get; set; } = 80;
        public int MemoryAlertThreshold { get; set; } = 85;
        public int DiskAlertThreshold { get; set; } = 90;
        public bool EnableAutoRefresh { get; set; } = true;
        public int MaxHistoryDays { get; set; } = 30;
        public string Theme { get; set; } = "Light";
    }

    /// <summary>
    /// Dashboard status summary
    /// </summary>
    public class DashboardStatus
    {
        public SystemHealth OverallHealth { get; set; }
        public int ActiveAlerts { get; set; }
        public int WarningCount { get; set; }
        public int CriticalCount { get; set; }
        public int ActiveUsers { get; set; }
        public DateTime LastUpdatedAt { get; set; }
    }

    public enum SystemHealth
    {
        Healthy = 0,
        Warning = 1,
        Critical = 2,
        Offline = 3
    }

    /// <summary>
    /// Quick action for dashboard controls
    /// </summary>
    public class QuickAction
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public string Icon { get; set; }
        public Func<Task> Action { get; set; }
        public bool IsEnabled { get; set; } = true;
        public string Category { get; set; }
    }

    /// <summary>
    /// Performance metric history
    /// </summary>
    public class MetricHistory
    {
        public DateTime Timestamp { get; set; }
        public string MetricName { get; set; }
        public double Value { get; set; }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public double AverageValue { get; set; }
    }
}
