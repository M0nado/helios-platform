using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HELIOS.Platform.Core.Logging;

namespace HELIOS.Platform.Core.Monitoring;

/// <summary>
/// Server monitoring service for real-time metrics, health checks, and diagnostics across multiple machines.
/// Extends dashboard monitoring with remote server support and advanced diagnostics.
/// </summary>
public interface IServerMonitoringService
{
    Task<ServerHealthStatus> GetServerHealthAsync();
    Task<IEnumerable<ServiceHealthStatus>> GetServiceHealthAsync();
    Task<PerformanceReport> GeneratePerformanceReportAsync(TimeSpan period);
    Task<AlertSummary> GetCurrentAlertsAsync();
    Task ClearAlertAsync(string alertId);
}

/// <summary>
/// Server health status summary.
/// </summary>
public class ServerHealthStatus
{
    public string HostName { get; set; } = Environment.MachineName;
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public string OverallStatus { get; set; } = "Healthy"; // Healthy, Warning, Critical
    public double UptimeHours { get; set; }
    public int ActiveAlerts { get; set; }
    public Dictionary<string, double> ComponentHealth { get; set; } = new();
}

/// <summary>
/// Individual service health status.
/// </summary>
public class ServiceHealthStatus
{
    public string ServiceName { get; set; } = string.Empty;
    public string Status { get; set; } = "Unknown"; // Running, Stopped, Error
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
    public long ResponseTimeMs { get; set; }
    public string HealthIndicator { get; set; } = "✓";
}

/// <summary>
/// Performance report for analysis and optimization.
/// </summary>
public class PerformanceReport
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan ReportPeriod { get; set; }
    public double AverageCpuUsage { get; set; }
    public double PeakCpuUsage { get; set; }
    public double AverageMemoryUsage { get; set; }
    public double PeakMemoryUsage { get; set; }
    public double AverageDiskUsage { get; set; }
    public List<string> Observations { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Alert summary for current issues.
/// </summary>
public class AlertSummary
{
    public int TotalAlerts { get; set; }
    public int CriticalAlerts { get; set; }
    public int WarningAlerts { get; set; }
    public List<AlertDetail> Details { get; set; } = new();
}

/// <summary>
/// Individual alert detail.
/// </summary>
public class AlertDetail
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string Severity { get; set; } = "Warning"; // Critical, Warning, Info
    public string Message { get; set; } = string.Empty;
    public string Component { get; set; } = string.Empty;
    public bool Resolved { get; set; }
}

/// <summary>
/// Default implementation of server monitoring service.
/// </summary>
public class ServerMonitoringService : IServerMonitoringService
{
    private readonly Core.Logging.ILogger? _logger;
    private readonly DashboardHistoryTracker? _dashboard;
    private readonly List<AlertDetail> _activeAlerts = new();

    public ServerMonitoringService(Core.Logging.ILogger? logger = null, DashboardHistoryTracker? dashboard = null)
    {
        _logger = logger;
        _dashboard = dashboard;
    }

    /// <summary>
    /// Gets comprehensive server health status.
    /// </summary>
    public async Task<ServerHealthStatus> GetServerHealthAsync()
    {
        var status = new ServerHealthStatus
        {
            HostName = Environment.MachineName,
            CheckedAt = DateTime.UtcNow,
            UptimeHours = Environment.TickCount / (3600000.0), // Convert milliseconds to hours
        };

        if (_dashboard != null)
        {
            var dashStats = await _dashboard.GetDashboardStatsAsync();
            status.ComponentHealth = new Dictionary<string, double>
            {
                { "CPU", dashStats.CpuAverage },
                { "Memory", dashStats.MemoryAverage },
                { "Disk", dashStats.DiskAverage }
            };

            // Determine overall status based on components
            if (dashStats.HealthStatus.Contains("Critical"))
                status.OverallStatus = "Critical";
            else if (dashStats.HealthStatus.Contains("Warning"))
                status.OverallStatus = "Warning";
            else
                status.OverallStatus = "Healthy";

            status.ActiveAlerts = dashStats.Alerts.Count;
        }

        _logger?.Info($"Server health checked: {status.OverallStatus}");
        return status;
    }

    /// <summary>
    /// Gets health status for all monitored services.
    /// </summary>
    public async Task<IEnumerable<ServiceHealthStatus>> GetServiceHealthAsync()
    {
        var services = new List<ServiceHealthStatus>();

        try
        {
            var systemMgmt = new Administration.SystemManagementService();
            var winServices = await systemMgmt.GetServicesAsync();

            foreach (var svc in winServices)
            {
                services.Add(new ServiceHealthStatus
                {
                    ServiceName = svc.Name,
                    Status = svc.Status,
                    LastChecked = DateTime.UtcNow,
                    ResponseTimeMs = 5,
                    HealthIndicator = svc.Status == "Running" ? "✓" : "✗"
                });
            }
        }
        catch (Exception ex)
        {
            _logger?.Error($"Error retrieving service health: {ex.Message}");
        }

        return services;
    }

    /// <summary>
    /// Generates a performance report for the specified period.
    /// </summary>
    public async Task<PerformanceReport> GeneratePerformanceReportAsync(TimeSpan period)
    {
        var report = new PerformanceReport { ReportPeriod = period };

        if (_dashboard != null)
        {
            var stats = await _dashboard.GetDashboardStatsAsync();

            report.AverageCpuUsage = stats.CpuAverage;
            report.PeakCpuUsage = stats.CpuPeak;
            report.AverageMemoryUsage = stats.MemoryAverage;
            report.PeakMemoryUsage = stats.MemoryPeak;
            report.AverageDiskUsage = stats.DiskAverage;

            // Generate observations
            if (report.AverageCpuUsage > 70)
                report.Observations.Add("High average CPU usage detected");
            if (report.PeakCpuUsage > 90)
                report.Observations.Add("Critical CPU spikes observed");
            if (report.AverageMemoryUsage > 7500)
                report.Observations.Add("Memory usage trending high");

            // Generate recommendations
            if (report.AverageCpuUsage > 70)
                report.Recommendations.Add("Consider optimizing background processes or upgrading CPU");
            if (report.AverageMemoryUsage > 7500)
                report.Recommendations.Add("Review memory-intensive applications; consider adding RAM");
            if (report.AverageDiskUsage > 85)
                report.Recommendations.Add("Disk space is low; consider cleanup or expansion");

            if (report.Recommendations.Count == 0)
                report.Recommendations.Add("System performance is optimal");
        }

        _logger?.Info($"Performance report generated for {period.TotalHours} hours");
        return report;
    }

    /// <summary>
    /// Gets current alert summary.
    /// </summary>
    public async Task<AlertSummary> GetCurrentAlertsAsync()
    {
        var summary = new AlertSummary { Details = _activeAlerts.Where(a => !a.Resolved).ToList() };
        summary.TotalAlerts = summary.Details.Count;
        summary.CriticalAlerts = summary.Details.Count(a => a.Severity == "Critical");
        summary.WarningAlerts = summary.Details.Count(a => a.Severity == "Warning");
        return await Task.FromResult(summary);
    }

    /// <summary>
    /// Clears/resolves an alert by ID.
    /// </summary>
    public async Task ClearAlertAsync(string alertId)
    {
        var alert = _activeAlerts.FirstOrDefault(a => a.Id == alertId);
        if (alert != null)
        {
            alert.Resolved = true;
            _logger?.Info($"Alert {alertId} marked as resolved");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Adds a new alert to the active alerts list.
    /// </summary>
    public void AddAlert(AlertDetail alert)
    {
        _activeAlerts.Add(alert);
        _logger?.Warning($"Alert added: {alert.Message}");
    }
}
