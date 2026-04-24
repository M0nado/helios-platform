using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HELIOS.Platform.Core.Logging;
using HELIOS.Platform.BackendServices.ServerManagement;

namespace HELIOS.Platform.Core.Monitoring
{
    /// <summary>
    /// Metric data point for historical tracking.
    /// </summary>
    public class MetricDataPoint
    {
        public DateTime Timestamp { get; set; }
        public double CpuUsagePercent { get; set; }
        public long MemoryUsageMB { get; set; }
        public int DiskUsagePercent { get; set; }
    }

    /// <summary>
    /// Dashboard history and analytics tracker.
    /// </summary>
    public interface IDashboardHistoryTracker
    {
        Task RecordMetricAsync(SystemResources resources);
        Task<List<MetricDataPoint>> GetMetricHistoryAsync(int minutes = 60);
        Task<DashboardStats> GetDashboardStatsAsync();
        void ClearOldData(int minutesToRetain = 120);
    }

    /// <summary>
    /// Dashboard statistics summary.
    /// </summary>
    public class DashboardStats
    {
        public double CpuAverage { get; set; }
        public double CpuPeak { get; set; }
        public double CpuMin { get; set; }
        public long MemoryAverage { get; set; }
        public long MemoryPeak { get; set; }
        public long MemoryMin { get; set; }
        public int DiskAverage { get; set; }
        public string HealthStatus { get; set; } = "Healthy";
        public List<string> Alerts { get; set; } = new();
    }

    /// <summary>
    /// Tracks dashboard metrics over time with history and alerts.
    /// </summary>
    public class DashboardHistoryTracker : IDashboardHistoryTracker
    {
        private readonly List<MetricDataPoint> _history = new();
        private readonly Core.Logging.ILogger _logger;
        private readonly object _lockObject = new();

        // Alert thresholds
        private const double CpuAlertThreshold = 85.0;
        private const long MemoryAlertThresholdMB = 7000;
        private const int DiskAlertThreshold = 90;

        public DashboardHistoryTracker()
        {
            _logger = new ConsoleLogger();
        }

        /// <summary>
        /// Record a metric data point.
        /// </summary>
        public async Task RecordMetricAsync(SystemResources resources)
        {
            await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    var dataPoint = new MetricDataPoint
                    {
                        Timestamp = DateTime.UtcNow,
                        CpuUsagePercent = resources.CpuUsagePercent,
                        MemoryUsageMB = resources.MemoryUsageMB,
                        DiskUsagePercent = resources.DiskUsagePercent
                    };

                    _history.Add(dataPoint);

                    // Auto-cleanup old data (keep last 2 hours)
                    ClearOldData(120);
                }
            });
        }

        /// <summary>
        /// Get metric history for specified time window.
        /// </summary>
        public async Task<List<MetricDataPoint>> GetMetricHistoryAsync(int minutes = 60)
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    var cutoffTime = DateTime.UtcNow.AddMinutes(-minutes);
                    return _history
                        .Where(h => h.Timestamp >= cutoffTime)
                        .OrderBy(h => h.Timestamp)
                        .ToList();
                }
            });
        }

        /// <summary>
        /// Get comprehensive dashboard statistics.
        /// </summary>
        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    if (!_history.Any())
                        return new DashboardStats { HealthStatus = "No Data" };

                    var stats = new DashboardStats
                    {
                        CpuAverage = _history.Average(h => h.CpuUsagePercent),
                        CpuPeak = _history.Max(h => h.CpuUsagePercent),
                        CpuMin = _history.Min(h => h.CpuUsagePercent),
                        MemoryAverage = Convert.ToInt64(_history.Average(h => h.MemoryUsageMB)),
                        MemoryPeak = _history.Max(h => h.MemoryUsageMB),
                        MemoryMin = _history.Min(h => h.MemoryUsageMB),
                        DiskAverage = Convert.ToInt32(_history.Average(h => h.DiskUsagePercent))
                    };

                    // Determine health status
                    stats.Alerts = GenerateAlerts(stats);
                    stats.HealthStatus = stats.Alerts.Count switch
                    {
                        0 => "✓ Healthy",
                        1 => "⚠ Warning",
                        _ => "✗ Critical"
                    };

                    return stats;
                }
            });
        }

        /// <summary>
        /// Clear historical data older than specified minutes.
        /// </summary>
        public void ClearOldData(int minutesToRetain = 120)
        {
            lock (_lockObject)
            {
                var cutoffTime = DateTime.UtcNow.AddMinutes(-minutesToRetain);
                _history.RemoveAll(h => h.Timestamp < cutoffTime);
            }
        }

        /// <summary>
        /// Generate alerts based on current stats.
        /// </summary>
        private static List<string> GenerateAlerts(DashboardStats stats)
        {
            var alerts = new List<string>();

            if (stats.CpuPeak > CpuAlertThreshold)
                alerts.Add($"⚠ CPU Peak: {stats.CpuPeak:F1}% (threshold: {CpuAlertThreshold}%)");

            if (stats.MemoryPeak > MemoryAlertThresholdMB)
                alerts.Add($"⚠ Memory Peak: {stats.MemoryPeak} MB (threshold: {MemoryAlertThresholdMB} MB)");

            if (stats.DiskAverage > DiskAlertThreshold)
                alerts.Add($"⚠ Disk Usage: {stats.DiskAverage}% (threshold: {DiskAlertThreshold}%)");

            if (stats.CpuAverage > 75)
                alerts.Add($"⚠ High CPU Average: {stats.CpuAverage:F1}%");

            return alerts;
        }
    }
}
