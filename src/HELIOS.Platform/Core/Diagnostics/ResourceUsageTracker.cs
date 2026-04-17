using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Diagnostics
{
    /// <summary>
    /// Tracks system and application resource usage
    /// </summary>
    public class ResourceUsageTracker
    {
        private static readonly ILogger _logger = Log.ForContext<ResourceUsageTracker>();
        private readonly Process _currentProcess = Process.GetCurrentProcess();
        private readonly PerformanceCounter _cpuCounter;
        private readonly List<ResourceSnapshot> _snapshots = new();
        private readonly object _lock = new();

        public ResourceUsageTracker()
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpuCounter.NextValue();
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to initialize CPU performance counter");
            }
        }

        /// <summary>
        /// Capture current resource snapshot
        /// </summary>
        public ResourceSnapshot CaptureSnapshot()
        {
            try
            {
                _currentProcess.Refresh();

                var snapshot = new ResourceSnapshot
                {
                    Timestamp = DateTime.UtcNow,
                    ProcessMemoryMB = _currentProcess.WorkingSet64 / (1024 * 1024),
                    VirtualMemoryMB = _currentProcess.VirtualMemorySize64 / (1024 * 1024),
                    CpuUsagePercent = GetCpuUsage(),
                    ThreadCount = _currentProcess.Threads.Count,
                    HandleCount = _currentProcess.HandleCount,
                    GCTotalMemoryMB = GC.GetTotalMemory(false) / (1024 * 1024),
                    GC0Collections = GC.CollectionCount(0),
                    GC1Collections = GC.CollectionCount(1),
                    GC2Collections = GC.CollectionCount(2)
                };

                lock (_lock)
                {
                    _snapshots.Add(snapshot);
                    // Keep last 1000 snapshots
                    if (_snapshots.Count > 1000)
                    {
                        _snapshots.RemoveAt(0);
                    }
                }

                return snapshot;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error capturing resource snapshot");
                return null;
            }
        }

        /// <summary>
        /// Get current CPU usage percentage
        /// </summary>
        private float GetCpuUsage()
        {
            try
            {
                if (_cpuCounter != null)
                {
                    return _cpuCounter.NextValue();
                }
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Error reading CPU counter");
            }

            return 0;
        }

        /// <summary>
        /// Get the latest resource snapshot
        /// </summary>
        public ResourceSnapshot GetLatestSnapshot()
        {
            lock (_lock)
            {
                return _snapshots.LastOrDefault();
            }
        }

        /// <summary>
        /// Get all snapshots within a time range
        /// </summary>
        public List<ResourceSnapshot> GetSnapshots(TimeSpan timeRange)
        {
            lock (_lock)
            {
                var cutoff = DateTime.UtcNow.Subtract(timeRange);
                return _snapshots.Where(s => s.Timestamp >= cutoff).ToList();
            }
        }

        /// <summary>
        /// Get resource usage statistics
        /// </summary>
        public ResourceUsageStatistics GetStatistics(TimeSpan? timeRange = null)
        {
            lock (_lock)
            {
                var snapshots = timeRange.HasValue
                    ? GetSnapshots(timeRange.Value)
                    : _snapshots;

                if (snapshots.Count == 0)
                    return new ResourceUsageStatistics();

                var stats = new ResourceUsageStatistics
                {
                    SampleCount = snapshots.Count,
                    StartTime = snapshots.First().Timestamp,
                    EndTime = snapshots.Last().Timestamp,
                    AverageProcessMemoryMB = snapshots.Average(s => s.ProcessMemoryMB),
                    MaxProcessMemoryMB = snapshots.Max(s => s.ProcessMemoryMB),
                    MinProcessMemoryMB = snapshots.Min(s => s.ProcessMemoryMB),
                    AverageCpuUsagePercent = snapshots.Average(s => s.CpuUsagePercent),
                    MaxCpuUsagePercent = snapshots.Max(s => s.CpuUsagePercent),
                    MinCpuUsagePercent = snapshots.Min(s => s.CpuUsagePercent),
                    AverageThreadCount = (int)snapshots.Average(s => s.ThreadCount),
                    MaxThreadCount = snapshots.Max(s => s.ThreadCount),
                    AverageHandleCount = (int)snapshots.Average(s => s.HandleCount),
                    MaxHandleCount = snapshots.Max(s => s.HandleCount),
                    AverageGCTotalMemoryMB = snapshots.Average(s => s.GCTotalMemoryMB),
                    MaxGCTotalMemoryMB = snapshots.Max(s => s.GCTotalMemoryMB)
                };

                return stats;
            }
        }

        /// <summary>
        /// Detect resource anomalies
        /// </summary>
        public ResourceAnomaly DetectAnomalies()
        {
            var anomaly = new ResourceAnomaly { DetectedAt = DateTime.UtcNow };
            var stats = GetStatistics(TimeSpan.FromMinutes(5));

            if (stats.SampleCount < 3)
                return anomaly;

            var latest = GetLatestSnapshot();
            if (latest == null)
                return anomaly;

            // Check for high memory usage
            if (latest.ProcessMemoryMB > stats.AverageProcessMemoryMB * 1.5)
            {
                anomaly.Anomalies.Add("High memory usage detected");
            }

            // Check for high CPU usage
            if (latest.CpuUsagePercent > stats.AverageCpuUsagePercent * 2)
            {
                anomaly.Anomalies.Add("High CPU usage detected");
            }

            // Check for thread leak
            if (latest.ThreadCount > stats.AverageThreadCount * 1.3)
            {
                anomaly.Anomalies.Add("Excessive thread count detected");
            }

            // Check for handle leak
            if (latest.HandleCount > stats.AverageHandleCount * 1.3)
            {
                anomaly.Anomalies.Add("Excessive handle count detected");
            }

            return anomaly;
        }

        /// <summary>
        /// Clear all snapshots
        /// </summary>
        public void ClearSnapshots()
        {
            lock (_lock)
            {
                _snapshots.Clear();
            }
        }
    }

    /// <summary>
    /// Snapshot of resource usage at a point in time
    /// </summary>
    public class ResourceSnapshot
    {
        public DateTime Timestamp { get; set; }
        public long ProcessMemoryMB { get; set; }
        public long VirtualMemoryMB { get; set; }
        public float CpuUsagePercent { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
        public long GCTotalMemoryMB { get; set; }
        public int GC0Collections { get; set; }
        public int GC1Collections { get; set; }
        public int GC2Collections { get; set; }
    }

    /// <summary>
    /// Resource usage statistics
    /// </summary>
    public class ResourceUsageStatistics
    {
        public int SampleCount { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double AverageProcessMemoryMB { get; set; }
        public long MaxProcessMemoryMB { get; set; }
        public long MinProcessMemoryMB { get; set; }
        public double AverageCpuUsagePercent { get; set; }
        public double MaxCpuUsagePercent { get; set; }
        public double MinCpuUsagePercent { get; set; }
        public int AverageThreadCount { get; set; }
        public int MaxThreadCount { get; set; }
        public int AverageHandleCount { get; set; }
        public int MaxHandleCount { get; set; }
        public double AverageGCTotalMemoryMB { get; set; }
        public long MaxGCTotalMemoryMB { get; set; }
    }

    /// <summary>
    /// Resource anomalies detected
    /// </summary>
    public class ResourceAnomaly
    {
        public DateTime DetectedAt { get; set; }
        public List<string> Anomalies { get; set; } = new();
        public bool HasAnomalies => Anomalies.Count > 0;
    }
}
