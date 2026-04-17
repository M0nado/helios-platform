using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;

namespace HELIOS.Platform.BackendServices.AI.Dashboard
{
    /// <summary>
    /// Real-time performance monitoring for AI components
    /// </summary>
    public interface IPerformanceMonitor
    {
        void RecordMetric(string componentId, PerformanceMetricPoint metric);
        Task<PerformanceStats> GetStatsAsync(string componentId);
        Task<PerformanceStats[]> GetAllStatsAsync();
        Task<HealthStatus> GetHealthStatusAsync();
        event EventHandler<PerformanceAlertEventArgs> AlertTriggered;
    }

    public class PerformanceMonitor : IPerformanceMonitor
    {
        private readonly ConcurrentDictionary<string, CircularBuffer<PerformanceMetricPoint>> _metrics;
        private readonly ConcurrentDictionary<string, ThresholdAlert> _thresholds;
        private readonly int _bufferSize;

        public event EventHandler<PerformanceAlertEventArgs> AlertTriggered;

        public PerformanceMonitor(int bufferSize = 1000)
        {
            _metrics = new ConcurrentDictionary<string, CircularBuffer<PerformanceMetricPoint>>();
            _thresholds = new ConcurrentDictionary<string, ThresholdAlert>();
            _bufferSize = bufferSize;
        }

        public void RecordMetric(string componentId, PerformanceMetricPoint metric)
        {
            var buffer = _metrics.GetOrAdd(componentId, _ => new CircularBuffer<PerformanceMetricPoint>(_bufferSize));
            buffer.Add(metric);

            CheckThresholds(componentId, metric);
        }

        public async Task<PerformanceStats> GetStatsAsync(string componentId)
        {
            if (!_metrics.TryGetValue(componentId, out var buffer))
            {
                return new PerformanceStats { ComponentId = componentId, RecordCount = 0 };
            }

            var points = buffer.ToArray();
            if (!points.Any())
            {
                return new PerformanceStats { ComponentId = componentId, RecordCount = 0 };
            }

            var latencies = points.Select(p => p.LatencyMs).OrderBy(l => l).ToList();
            var memory = points.Select(p => p.MemoryMb).ToList();
            var cpu = points.Select(p => p.CpuPercent).ToList();

            return await Task.FromResult(new PerformanceStats
            {
                ComponentId = componentId,
                RecordCount = points.Length,
                LatencyStats = new LatencyStatistics
                {
                    Min = latencies.First(),
                    Max = latencies.Last(),
                    Average = latencies.Average(),
                    P50 = GetPercentile(latencies, 0.5),
                    P95 = GetPercentile(latencies, 0.95),
                    P99 = GetPercentile(latencies, 0.99)
                },
                MemoryStats = new ResourceStatistics
                {
                    Current = memory.Last(),
                    Average = memory.Average(),
                    Peak = memory.Max(),
                    Min = memory.Min()
                },
                CpuStats = new ResourceStatistics
                {
                    Current = cpu.Last(),
                    Average = cpu.Average(),
                    Peak = cpu.Max(),
                    Min = cpu.Min()
                },
                SuccessRate = points.Count(p => p.IsSuccess) / (double)points.Length,
                ErrorCount = points.Count(p => !p.IsSuccess),
                ThroughputTasksPerSecond = CalculateThroughput(points),
                LastUpdated = DateTime.UtcNow
            });
        }

        public async Task<PerformanceStats[]> GetAllStatsAsync()
        {
            var tasks = _metrics.Keys.Select(componentId => GetStatsAsync(componentId));
            var results = await Task.WhenAll(tasks);
            return results;
        }

        public async Task<HealthStatus> GetHealthStatusAsync()
        {
            var allStats = await GetAllStatsAsync();
            
            var criticalIssues = 0;
            var warnings = 0;
            var issues = new List<string>();

            foreach (var stat in allStats)
            {
                if (stat.LatencyStats?.P99 > 10000)
                {
                    criticalIssues++;
                    issues.Add($"{stat.ComponentId}: P99 latency exceeds 10s");
                }

                if (stat.SuccessRate < 0.95)
                {
                    warnings++;
                    issues.Add($"{stat.ComponentId}: Success rate below 95%");
                }

                if (stat.MemoryStats?.Peak > 16000)
                {
                    warnings++;
                    issues.Add($"{stat.ComponentId}: Peak memory exceeds 16GB");
                }
            }

            var status = criticalIssues > 0 ? HealthStatus.Critical 
                       : warnings > 0 ? HealthStatus.Warning 
                       : HealthStatus.Healthy;

            return await Task.FromResult(new HealthStatus
            {
                Status = status,
                IssueCount = criticalIssues + warnings,
                Issues = issues,
                CheckedAt = DateTime.UtcNow
            });
        }

        public void SetThreshold(string componentId, ThresholdAlert threshold)
        {
            _thresholds.AddOrUpdate(componentId, threshold, (_, _) => threshold);
        }

        private void CheckThresholds(string componentId, PerformanceMetricPoint metric)
        {
            if (!_thresholds.TryGetValue(componentId, out var threshold))
                return;

            var alerts = new List<string>();

            if (metric.LatencyMs > threshold.LatencyMsMax)
            {
                alerts.Add($"Latency {metric.LatencyMs}ms exceeds threshold {threshold.LatencyMsMax}ms");
            }

            if (metric.MemoryMb > threshold.MemoryMbMax)
            {
                alerts.Add($"Memory {metric.MemoryMb}MB exceeds threshold {threshold.MemoryMbMax}MB");
            }

            if (metric.CpuPercent > threshold.CpuPercentMax)
            {
                alerts.Add($"CPU {metric.CpuPercent}% exceeds threshold {threshold.CpuPercentMax}%");
            }

            if (!metric.IsSuccess && threshold.FailureAlertEnabled)
            {
                alerts.Add($"Component failed: {metric.FailureReason}");
            }

            if (alerts.Any())
            {
                AlertTriggered?.Invoke(this, new PerformanceAlertEventArgs
                {
                    ComponentId = componentId,
                    AlertMessage = string.Join("; ", alerts),
                    Severity = alerts.Any(a => a.Contains("Latency") || a.Contains("fail")) 
                        ? AlertSeverity.Critical 
                        : AlertSeverity.Warning,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        private double GetPercentile(List<double> sortedValues, double percentile)
        {
            if (!sortedValues.Any()) return 0;
            var index = (int)Math.Ceiling(percentile * sortedValues.Count) - 1;
            return sortedValues[Math.Max(0, Math.Min(index, sortedValues.Count - 1))];
        }

        private double CalculateThroughput(PerformanceMetricPoint[] points)
        {
            if (points.Length < 2) return 0;
            
            var timeSpan = points.Last().Timestamp - points.First().Timestamp;
            var seconds = timeSpan.TotalSeconds;
            
            return seconds > 0 ? points.Length / seconds : 0;
        }
    }

    /// <summary>
    /// Circular buffer for efficient metric storage
    /// </summary>
    public class CircularBuffer<T>
    {
        private readonly T[] _buffer;
        private int _head;
        private int _count;

        public CircularBuffer(int capacity)
        {
            _buffer = new T[capacity];
            _head = 0;
            _count = 0;
        }

        public void Add(T item)
        {
            _buffer[_head] = item;
            _head = (_head + 1) % _buffer.Length;
            if (_count < _buffer.Length)
                _count++;
        }

        public T[] ToArray()
        {
            var result = new T[_count];
            for (int i = 0; i < _count; i++)
            {
                result[i] = _buffer[((_head - _count + i) + _buffer.Length) % _buffer.Length];
            }
            return result;
        }
    }

    public class PerformanceMetricPoint
    {
        public DateTime Timestamp { get; set; }
        public double LatencyMs { get; set; }
        public double MemoryMb { get; set; }
        public double CpuPercent { get; set; }
        public bool IsSuccess { get; set; }
        public string FailureReason { get; set; }
        public long ThroughputTasksPerSec { get; set; }
        public int QueuedTasks { get; set; }
    }

    public class PerformanceStats
    {
        public string ComponentId { get; set; }
        public int RecordCount { get; set; }
        public LatencyStatistics LatencyStats { get; set; }
        public ResourceStatistics MemoryStats { get; set; }
        public ResourceStatistics CpuStats { get; set; }
        public double SuccessRate { get; set; }
        public int ErrorCount { get; set; }
        public double ThroughputTasksPerSecond { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class LatencyStatistics
    {
        public double Min { get; set; }
        public double Max { get; set; }
        public double Average { get; set; }
        public double P50 { get; set; }
        public double P95 { get; set; }
        public double P99 { get; set; }
    }

    public class ResourceStatistics
    {
        public double Current { get; set; }
        public double Average { get; set; }
        public double Peak { get; set; }
        public double Min { get; set; }
    }

    public class ThresholdAlert
    {
        public double LatencyMsMax { get; set; } = 5000;
        public double MemoryMbMax { get; set; } = 16000;
        public double CpuPercentMax { get; set; } = 90;
        public bool FailureAlertEnabled { get; set; } = true;
    }

    public class HealthStatus
    {
        public HealthStatus Status { get; set; }
        public int IssueCount { get; set; }
        public List<string> Issues { get; set; }
        public DateTime CheckedAt { get; set; }
    }

    public class PerformanceAlertEventArgs : EventArgs
    {
        public string ComponentId { get; set; }
        public string AlertMessage { get; set; }
        public AlertSeverity Severity { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public enum AlertSeverity
    {
        Info,
        Warning,
        Critical
    }
}
