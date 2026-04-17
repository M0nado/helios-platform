using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HELIOS.Platform.Core.Diagnostics
{
    /// <summary>
    /// Provides data for system health dashboard
    /// </summary>
    public class HealthDashboardProvider
    {
        private static readonly ILogger _logger = Log.ForContext<HealthDashboardProvider>();
        private readonly HealthDiagnosticsEngine _diagnosticsEngine;
        private readonly ResourceUsageTracker _resourceTracker;
        private readonly PerformanceMonitor _performanceMonitor;
        private readonly HealthAlertSystem _alertSystem;
        private readonly CustomPerformanceCounterManager _counterManager;

        public HealthDashboardProvider(
            HealthDiagnosticsEngine diagnosticsEngine,
            ResourceUsageTracker resourceTracker,
            PerformanceMonitor performanceMonitor,
            HealthAlertSystem alertSystem,
            CustomPerformanceCounterManager counterManager)
        {
            _diagnosticsEngine = diagnosticsEngine;
            _resourceTracker = resourceTracker;
            _performanceMonitor = performanceMonitor;
            _alertSystem = alertSystem;
            _counterManager = counterManager;
        }

        /// <summary>
        /// Generate comprehensive dashboard data
        /// </summary>
        public DashboardData GenerateDashboardData()
        {
            try
            {
                var dashboard = new DashboardData { GeneratedAt = DateTime.UtcNow };

                // Get overall health status
                dashboard.HealthStatus = _diagnosticsEngine.GetAllCachedStatuses().FirstOrDefault() ?? new HealthStatus();

                // Get resource usage
                var resourceSnapshot = _resourceTracker.GetLatestSnapshot();
                if (resourceSnapshot != null)
                {
                    dashboard.ResourceUsage = new ResourceUsageData
                    {
                        ProcessMemoryMB = resourceSnapshot.ProcessMemoryMB,
                        CpuUsagePercent = resourceSnapshot.CpuUsagePercent,
                        ThreadCount = resourceSnapshot.ThreadCount,
                        HandleCount = resourceSnapshot.HandleCount,
                        Timestamp = resourceSnapshot.Timestamp
                    };
                }

                // Get performance metrics
                foreach (var counter in _performanceMonitor.GetAllCounters())
                {
                    dashboard.PerformanceMetrics.Add(counter.GetSnapshot());
                }

                // Get custom counters
                foreach (var counter in _counterManager.GetAllCounters())
                {
                    dashboard.CustomCounters.Add(counter.ToSnapshot());
                }

                // Get active alerts
                dashboard.ActiveAlerts = _alertSystem.GetActiveAlerts();

                // Get alert statistics
                dashboard.AlertStatistics = _alertSystem.GetStatistics();

                // Get resource statistics for the last hour
                var hourlyStats = _resourceTracker.GetStatistics(TimeSpan.FromHours(1));
                dashboard.ResourceStatistics = new ResourceUsageStatisticsData
                {
                    AverageProcessMemoryMB = hourlyStats.AverageProcessMemoryMB,
                    MaxProcessMemoryMB = hourlyStats.MaxProcessMemoryMB,
                    AverageCpuUsagePercent = hourlyStats.AverageCpuUsagePercent,
                    MaxCpuUsagePercent = hourlyStats.MaxCpuUsagePercent,
                    SampleCount = hourlyStats.SampleCount
                };

                _logger.Debug("Dashboard data generated successfully");
                return dashboard;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error generating dashboard data");
                return new DashboardData { GeneratedAt = DateTime.UtcNow };
            }
        }

        /// <summary>
        /// Generate a summary dashboard
        /// </summary>
        public DashboardSummary GenerateSummary()
        {
            var fullData = GenerateDashboardData();
            return new DashboardSummary
            {
                GeneratedAt = fullData.GeneratedAt,
                HealthStatus = fullData.HealthStatus.Status,
                CurrentMemoryMB = fullData.ResourceUsage?.ProcessMemoryMB ?? 0,
                CurrentCpuPercent = fullData.ResourceUsage?.CpuUsagePercent ?? 0,
                ActiveAlertCount = fullData.ActiveAlerts.Count,
                CriticalAlertCount = fullData.ActiveAlerts.Count(a => a.Severity == AlertSeverity.Critical),
                AverageMemoryMB = fullData.ResourceStatistics?.AverageProcessMemoryMB ?? 0,
                AverageCpuPercent = fullData.ResourceStatistics?.AverageCpuUsagePercent ?? 0
            };
        }

        /// <summary>
        /// Generate health trend data
        /// </summary>
        public HealthTrendData GenerateHealthTrend(TimeSpan? timeRange = null)
        {
            timeRange ??= TimeSpan.FromHours(1);
            var snapshots = _resourceTracker.GetSnapshots(timeRange.Value);

            var trend = new HealthTrendData
            {
                TimeRange = timeRange.Value,
                GeneratedAt = DateTime.UtcNow
            };

            foreach (var snapshot in snapshots.OrderBy(s => s.Timestamp))
            {
                trend.TimePoints.Add(new HealthTrendPoint
                {
                    Timestamp = snapshot.Timestamp,
                    MemoryMB = snapshot.ProcessMemoryMB,
                    CpuPercent = snapshot.CpuUsagePercent,
                    ThreadCount = snapshot.ThreadCount
                });
            }

            return trend;
        }
    }

    /// <summary>
    /// Complete dashboard data
    /// </summary>
    public class DashboardData
    {
        public DateTime GeneratedAt { get; set; }
        public HealthStatus HealthStatus { get; set; }
        public ResourceUsageData ResourceUsage { get; set; }
        public List<PerformanceCounterSnapshot> PerformanceMetrics { get; set; } = new();
        public List<CustomCounterSnapshot> CustomCounters { get; set; } = new();
        public List<Alert> ActiveAlerts { get; set; } = new();
        public AlertStatistics AlertStatistics { get; set; }
        public ResourceUsageStatisticsData ResourceStatistics { get; set; }
    }

    /// <summary>
    /// Resource usage data for dashboard
    /// </summary>
    public class ResourceUsageData
    {
        public long ProcessMemoryMB { get; set; }
        public float CpuUsagePercent { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Resource usage statistics for dashboard
    /// </summary>
    public class ResourceUsageStatisticsData
    {
        public double AverageProcessMemoryMB { get; set; }
        public long MaxProcessMemoryMB { get; set; }
        public double AverageCpuUsagePercent { get; set; }
        public double MaxCpuUsagePercent { get; set; }
        public int SampleCount { get; set; }
    }

    /// <summary>
    /// Dashboard summary
    /// </summary>
    public class DashboardSummary
    {
        public DateTime GeneratedAt { get; set; }
        public HealthStatusCode HealthStatus { get; set; }
        public long CurrentMemoryMB { get; set; }
        public float CurrentCpuPercent { get; set; }
        public int ActiveAlertCount { get; set; }
        public int CriticalAlertCount { get; set; }
        public double AverageMemoryMB { get; set; }
        public double AverageCpuPercent { get; set; }
    }

    /// <summary>
    /// Health trend data
    /// </summary>
    public class HealthTrendData
    {
        public DateTime GeneratedAt { get; set; }
        public TimeSpan TimeRange { get; set; }
        public List<HealthTrendPoint> TimePoints { get; set; } = new();
    }

    /// <summary>
    /// Single point in health trend
    /// </summary>
    public class HealthTrendPoint
    {
        public DateTime Timestamp { get; set; }
        public long MemoryMB { get; set; }
        public float CpuPercent { get; set; }
        public int ThreadCount { get; set; }
    }
}
