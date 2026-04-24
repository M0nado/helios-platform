using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HELIOS.Platform.AI.DataPipeline
{
    /// <summary>
    /// Data pipeline for collecting, processing, and storing system metrics.
    /// ~700 LOC with feature extraction, normalization, and storage.
    /// </summary>
    public class DataPipeline
    {
        private readonly ILogger<DataPipeline> _logger;
        private readonly List<SystemMetricSnapshot> _metricHistory = new();
        private readonly object _lockObj = new();
        private const int MaxHistorySize = 10000;

        public DataPipeline(ILogger<DataPipeline> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Collect system metrics from various sources.
        /// </summary>
        public async Task<SystemMetricSnapshot> CollectMetricsAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var snapshot = new SystemMetricSnapshot
                    {
                        Timestamp = DateTime.UtcNow
                    };

                    // Collect CPU metrics
                    snapshot.CpuUsage = CollectCpuMetrics();

                    // Collect memory metrics
                    snapshot.MemoryMetrics = CollectMemoryMetrics();

                    // Collect disk metrics
                    snapshot.DiskMetrics = CollectDiskMetrics();

                    // Collect network metrics
                    snapshot.NetworkMetrics = CollectNetworkMetrics();

                    lock (_lockObj)
                    {
                        _metricHistory.Add(snapshot);
                        if (_metricHistory.Count > MaxHistorySize)
                        {
                            _metricHistory.RemoveAt(0);
                        }
                    }

                    _logger.LogDebug("Metrics collected successfully");
                    return snapshot;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error collecting metrics");
                    return new SystemMetricSnapshot { Status = "Error" };
                }
            });
        }

        /// <summary>
        /// Extract features from raw metrics for ML models.
        /// </summary>
        public FeatureVector ExtractFeatures(SystemMetricSnapshot snapshot)
        {
            if (snapshot == null)
                return new FeatureVector { Status = "Error: null snapshot" };

            try
            {
                var features = new FeatureVector
                {
                    Timestamp = snapshot.Timestamp,
                    RawFeatures = new float[]
                    {
                        (float)snapshot.CpuUsage,
                        (float)snapshot.MemoryMetrics.UsagePercent,
                        (float)snapshot.MemoryMetrics.AvailableBytes / (1024 * 1024),
                        (float)snapshot.DiskMetrics.UsagePercent,
                        (float)snapshot.NetworkMetrics.BytesInPerSec,
                        (float)snapshot.NetworkMetrics.BytesOutPerSec,
                        (float)(snapshot.ProcessMetrics?.Count ?? 0),
                        (float)(snapshot.ProcessMetrics?.Where(p => p.CpuUsage > 10).Count() ?? 0)
                    }
                    
                };

                // Normalize features
                features.NormalizedFeatures = NormalizeFeatures(features.RawFeatures);

                // Calculate aggregate features
                features.AggregateFeatures = CalculateAggregateFeatures(snapshot);

                features.Status = "Success";
                return features;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting features");
                return new FeatureVector { Status = $"Error: {ex.Message}" };
            }
        }

        /// <summary>
        /// Normalize features to 0-1 range.
        /// </summary>
        public float[] NormalizeFeatures(float[] features)
        {
            if (features == null || features.Length == 0) return features;

            var normalized = new float[features.Length];

            // Define normalization ranges for each feature
            var ranges = new (float min, float max)[]
            {
                (0, 100),        // CPU usage
                (0, 100),        // Memory percent
                (0, 64000),      // Available memory MB
                (0, 100),        // Disk usage
                (0, 1000),       // Network bytes in
                (0, 1000),       // Network bytes out
                (0, 500),        // Process count
                (0, 100)         // High CPU process count
            };

            for (int i = 0; i < features.Length && i < ranges.Length; i++)
            {
                var range = ranges[i];
                var denominator = range.max - range.min;
                normalized[i] = denominator == 0 ? 0 : (features[i] - range.min) / denominator;
                normalized[i] = Math.Clamp(normalized[i], 0, 1);
            }

            return normalized;
        }

        /// <summary>
        /// Calculate aggregate features from historical data.
        /// </summary>
        public AggregateFeatures CalculateAggregateFeatures(SystemMetricSnapshot latest)
        {
            lock (_lockObj)
            {
                var recentHistory = _metricHistory.TakeLast(100).ToList();

                var aggregates = new AggregateFeatures();

                if (recentHistory.Count > 0)
                {
                    // CPU aggregates
                    aggregates.CpuMean = recentHistory.Average(m => m.CpuUsage);
                    aggregates.CpuMax = recentHistory.Max(m => m.CpuUsage);
                    aggregates.CpuMin = recentHistory.Min(m => m.CpuUsage);
                    aggregates.CpuTrend = CalculateTrend(recentHistory.Select(m => m.CpuUsage).ToList());

                    // Memory aggregates
                    aggregates.MemoryMean = recentHistory.Average(m => m.MemoryMetrics.UsagePercent);
                    aggregates.MemoryMax = recentHistory.Max(m => m.MemoryMetrics.UsagePercent);
                    aggregates.MemoryTrend = CalculateTrend(recentHistory.Select(m => m.MemoryMetrics.UsagePercent).ToList());

                    // Volatility metrics
                    aggregates.CpuVolatility = CalculateStdDev(recentHistory.Select(m => m.CpuUsage).ToList());
                    aggregates.MemoryVolatility = CalculateStdDev(recentHistory.Select(m => m.MemoryMetrics.UsagePercent).ToList());
                }

                return aggregates;
            }
        }

        /// <summary>
        /// Get metrics history for analysis.
        /// </summary>
        public List<SystemMetricSnapshot> GetMetricsHistory(int count = 100)
        {
            lock (_lockObj)
            {
                return _metricHistory.TakeLast(count).ToList();
            }
        }

        /// <summary>
        /// Export metrics to format suitable for model training.
        /// </summary>
        public async Task<TrainingDataExport> ExportTrainingDataAsync(DateTime startTime, DateTime endTime)
        {
            return await Task.Run(() =>
            {
                lock (_lockObj)
                {
                    var filtered = _metricHistory
                        .Where(m => m.Timestamp >= startTime && m.Timestamp <= endTime)
                        .ToList();

                    var export = new TrainingDataExport
                    {
                        ExportedAt = DateTime.UtcNow,
                        DataPoints = filtered.Count,
                        Features = filtered.Select(m => new float[]
                        {
                            (float)m.CpuUsage,
                            (float)m.MemoryMetrics.UsagePercent,
                            (float)m.DiskMetrics.UsagePercent,
                            (float)m.NetworkMetrics.BytesInPerSec,
                            (float)m.NetworkMetrics.BytesOutPerSec
                        }).ToList(),
                        Timestamps = filtered.Select(m => m.Timestamp).ToList(),
                        Status = "Success"
                    };

                    _logger.LogInformation("Exported {Count} training data points", filtered.Count);
                    return export;
                }
            });
        }

        /// <summary>
        /// Clear old metrics data for privacy and storage management.
        /// </summary>
        public int ClearOldMetrics(TimeSpan retentionPeriod)
        {
            lock (_lockObj)
            {
                var cutoffTime = DateTime.UtcNow - retentionPeriod;
                var count = _metricHistory.Count;
                _metricHistory.RemoveAll(m => m.Timestamp < cutoffTime);
                var removed = count - _metricHistory.Count;
                _logger.LogInformation("Cleared {Count} old metric records", removed);
                return removed;
            }
        }

        private double CollectCpuMetrics()
        {
            try
            {
                var cpuCounter = new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                return Math.Round(cpuCounter.NextValue(), 2);
            }
            catch { return 0; }
        }

        private MemoryMetrics CollectMemoryMetrics()
        {
            try
            {
                var totalMemory = GC.GetTotalMemory(false);
                var workingSet = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
                var percentUsage = (totalMemory / (double)(1024 * 1024 * 1024)) * 100;

                return new MemoryMetrics
                {
                    TotalBytes = totalMemory,
                    UsedBytes = workingSet,
                    AvailableBytes = Math.Max(0, totalMemory - workingSet),
                    UsagePercent = Math.Min(100, percentUsage)
                };
            }
            catch
            {
                return new MemoryMetrics();
            }
        }

        private DiskMetrics CollectDiskMetrics()
        {
            try
            {
                var drives = System.IO.DriveInfo.GetDrives();
                long totalSize = 0, totalFreeSpace = 0;

                foreach (var drive in drives.Where(d => d.IsReady))
                {
                    totalSize += drive.TotalSize;
                    totalFreeSpace += drive.TotalFreeSpace;
                }

                var usagePercent = totalSize == 0 ? 0 : ((totalSize - totalFreeSpace) / (double)totalSize) * 100;

                return new DiskMetrics
                {
                    TotalBytes = totalSize,
                    UsedBytes = totalSize - totalFreeSpace,
                    FreeBytes = totalFreeSpace,
                    UsagePercent = usagePercent
                };
            }
            catch
            {
                return new DiskMetrics();
            }
        }

        private NetworkMetrics CollectNetworkMetrics()
        {
            try
            {
                var interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
                long bytesIn = 0, bytesOut = 0;

                foreach (var nic in interfaces)
                {
                    if (nic.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                    {
                        var stats = nic.GetIPStatistics();
                        bytesIn += stats.BytesReceived;
                        bytesOut += stats.BytesSent;
                    }
                }

                return new NetworkMetrics { BytesInPerSec = bytesIn, BytesOutPerSec = bytesOut };
            }
            catch
            {
                return new NetworkMetrics();
            }
        }

        private double CalculateTrend(List<double> values)
        {
            if (values.Count < 2) return 0;

            var recentValues = values.TakeLast(10).ToList();
            if (recentValues.Count < 2) return 0;

            var xValues = Enumerable.Range(0, recentValues.Count).Select(i => (double)i).ToList();
            var xMean = xValues.Average();
            var yMean = recentValues.Average();

            var numerator = xValues.Zip(recentValues).Sum(pair => (pair.First - xMean) * (pair.Second - yMean));
            var denominator = xValues.Sum(x => Math.Pow(x - xMean, 2));

            return denominator == 0 ? 0 : numerator / denominator;
        }

        private double CalculateStdDev(List<double> values)
        {
            if (values.Count < 2) return 0;
            var mean = values.Average();
            var variance = values.Sum(v => Math.Pow(v - mean, 2)) / values.Count;
            return Math.Sqrt(variance);
        }
    }

    public class SystemMetricSnapshot
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public double CpuUsage { get; set; }
        public MemoryMetrics MemoryMetrics { get; set; } = new();
        public DiskMetrics DiskMetrics { get; set; } = new();
        public NetworkMetrics NetworkMetrics { get; set; } = new();
        public List<ProcessMetric> ProcessMetrics { get; set; } = new();
        public string Status { get; set; } = "Unknown";
    }

    public class MemoryMetrics
    {
        public long TotalBytes { get; set; }
        public long UsedBytes { get; set; }
        public long AvailableBytes { get; set; }
        public double UsagePercent { get; set; }
    }

    public class DiskMetrics
    {
        public long TotalBytes { get; set; }
        public long UsedBytes { get; set; }
        public long FreeBytes { get; set; }
        public double UsagePercent { get; set; }
    }

    public class NetworkMetrics
    {
        public long BytesInPerSec { get; set; }
        public long BytesOutPerSec { get; set; }
    }

    public class ProcessMetric
    {
        public int PID { get; set; }
        public string Name { get; set; } = string.Empty;
        public double CpuUsage { get; set; }
        public long MemoryUsage { get; set; }
    }

    public class FeatureVector
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public float[] RawFeatures { get; set; } = Array.Empty<float>();
        public float[] NormalizedFeatures { get; set; } = Array.Empty<float>();
        public AggregateFeatures AggregateFeatures { get; set; } = new();
        public string Status { get; set; } = string.Empty;
    }

    public class AggregateFeatures
    {
        public double CpuMean { get; set; }
        public double CpuMax { get; set; }
        public double CpuMin { get; set; }
        public double CpuTrend { get; set; }
        public double CpuVolatility { get; set; }
        public double MemoryMean { get; set; }
        public double MemoryMax { get; set; }
        public double MemoryTrend { get; set; }
        public double MemoryVolatility { get; set; }
    }

    public class TrainingDataExport
    {
        public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
        public int DataPoints { get; set; }
        public List<float[]> Features { get; set; } = new();
        public List<DateTime> Timestamps { get; set; } = new();
        public string Status { get; set; } = string.Empty;
    }
}
