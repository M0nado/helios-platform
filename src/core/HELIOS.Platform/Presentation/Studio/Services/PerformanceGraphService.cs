using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HELIOS.Platform.Presentation.Studio.Models;

namespace HELIOS.Platform.Presentation.Studio.Services
{
    /// <summary>
    /// Manages performance graphs and historical data visualization
    /// Supports multiple time ranges: 1h, 24h, 7d
    /// </summary>
    public class PerformanceGraphService
    {
        private readonly Dictionary<string, List<MetricHistory>> _metricHistory;
        private readonly int _maxHistoryEntries = 10000;

        public PerformanceGraphService()
        {
            _metricHistory = new Dictionary<string, List<MetricHistory>>();
        }

        /// <summary>
        /// Record a metric point in history
        /// </summary>
        public void RecordMetric(string metricName, double value)
        {
            if (!_metricHistory.ContainsKey(metricName))
                _metricHistory[metricName] = new List<MetricHistory>();

            var history = _metricHistory[metricName];
            
            var entry = new MetricHistory
            {
                Timestamp = DateTime.UtcNow,
                MetricName = metricName,
                Value = value
            };

            history.Add(entry);

            // Maintain max size
            if (history.Count > _maxHistoryEntries)
                history.RemoveRange(0, history.Count - _maxHistoryEntries);

            // Update aggregates
            UpdateAggregates(metricName, history);
        }

        /// <summary>
        /// Record metrics from SystemMetrics snapshot
        /// </summary>
        public void RecordMetricsSnapshot(SystemMetrics metrics)
        {
            RecordMetric("CPU", metrics.CpuUsagePercent);
            RecordMetric("Memory", metrics.MemoryUsagePercent);
            RecordMetric("Disk", metrics.DiskUsagePercent);
            RecordMetric("Network", metrics.NetworkBytesPerSecond);
            if (metrics.GpuUsagePercent > 0)
                RecordMetric("GPU", metrics.GpuUsagePercent);
        }

        /// <summary>
        /// Get graph data for the last hour
        /// </summary>
        public List<MetricHistory> GetHourlyGraph(string metricName)
        {
            var cutoff = DateTime.UtcNow.AddHours(-1);
            return GetMetricData(metricName, cutoff);
        }

        /// <summary>
        /// Get graph data for the last 24 hours
        /// </summary>
        public List<MetricHistory> GetDailyGraph(string metricName)
        {
            var cutoff = DateTime.UtcNow.AddHours(-24);
            return GetMetricData(metricName, cutoff);
        }

        /// <summary>
        /// Get graph data for the last 7 days
        /// </summary>
        public List<MetricHistory> GetWeeklyGraph(string metricName)
        {
            var cutoff = DateTime.UtcNow.AddDays(-7);
            return GetMetricData(metricName, cutoff);
        }

        /// <summary>
        /// Get all available metrics
        /// </summary>
        public IEnumerable<string> GetAvailableMetrics()
        {
            return _metricHistory.Keys;
        }

        /// <summary>
        /// Get statistics for a metric
        /// </summary>
        public MetricStatistics GetMetricStatistics(string metricName, TimeSpan timeRange)
        {
            if (!_metricHistory.ContainsKey(metricName))
                return null;

            var cutoff = DateTime.UtcNow.Subtract(timeRange);
            var data = _metricHistory[metricName]
                .Where(m => m.Timestamp >= cutoff)
                .ToList();

            if (!data.Any())
                return null;

            var values = data.Select(d => d.Value).ToList();

            return new MetricStatistics
            {
                MetricName = metricName,
                CurrentValue = values.Last(),
                MinValue = values.Min(),
                MaxValue = values.Max(),
                AverageValue = values.Average(),
                MedianValue = GetMedian(values),
                StandardDeviation = CalculateStandardDeviation(values),
                DataPointCount = data.Count,
                TimeRangeHours = timeRange.TotalHours
            };
        }

        /// <summary>
        /// Clear history older than specified days
        /// </summary>
        public void ClearOldData(int daysToKeep = 30)
        {
            var cutoff = DateTime.UtcNow.AddDays(-daysToKeep);

            foreach (var kvp in _metricHistory)
            {
                kvp.Value.RemoveAll(m => m.Timestamp < cutoff);
            }
        }

        /// <summary>
        /// Export metrics as CSV
        /// </summary>
        public string ExportAsCSV(string metricName, TimeSpan timeRange)
        {
            if (!_metricHistory.ContainsKey(metricName))
                return string.Empty;

            var cutoff = DateTime.UtcNow.Subtract(timeRange);
            var data = _metricHistory[metricName]
                .Where(m => m.Timestamp >= cutoff)
                .ToList();

            var csv = "Timestamp,MetricName,Value\n";
            foreach (var entry in data)
            {
                csv += $"{entry.Timestamp:O},{entry.MetricName},{entry.Value}\n";
            }

            return csv;
        }

        /// <summary>
        /// Calculate trends and anomalies
        /// </summary>
        public TrendAnalysis AnalyzeTrends(string metricName, int periods = 24)
        {
            if (!_metricHistory.ContainsKey(metricName))
                return null;

            var data = _metricHistory[metricName]
                .OrderByDescending(m => m.Timestamp)
                .Take(periods * 2)
                .OrderBy(m => m.Timestamp)
                .ToList();

            if (data.Count < periods)
                return null;

            var firstPeriod = data.Take(periods).Select(d => d.Value).ToList();
            var secondPeriod = data.Skip(periods).Take(periods).Select(d => d.Value).ToList();

            var firstAvg = firstPeriod.Average();
            var secondAvg = secondPeriod.Average();
            var percentChange = ((secondAvg - firstAvg) / firstAvg) * 100;

            return new TrendAnalysis
            {
                MetricName = metricName,
                PreviousPeriodAverage = firstAvg,
                CurrentPeriodAverage = secondAvg,
                PercentChange = percentChange,
                Trend = percentChange > 2 ? "Up" : percentChange < -2 ? "Down" : "Stable"
            };
        }

        // Helper methods

        private List<MetricHistory> GetMetricData(string metricName, DateTime cutoff)
        {
            if (!_metricHistory.ContainsKey(metricName))
                return new List<MetricHistory>();

            return _metricHistory[metricName]
                .Where(m => m.Timestamp >= cutoff)
                .ToList();
        }

        private void UpdateAggregates(string metricName, List<MetricHistory> history)
        {
            if (history.Count < 2)
                return;

            var lastEntry = history.Last();
            var previousEntries = history.Skip(Math.Max(0, history.Count - 60)).ToList();

            lastEntry.MinValue = previousEntries.Min(h => h.Value);
            lastEntry.MaxValue = previousEntries.Max(h => h.Value);
            lastEntry.AverageValue = previousEntries.Average(h => h.Value);
        }

        private double GetMedian(List<double> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            int n = sorted.Count;
            if (n % 2 == 0)
                return (sorted[n / 2 - 1] + sorted[n / 2]) / 2;
            return sorted[n / 2];
        }

        private double CalculateStandardDeviation(List<double> values)
        {
            if (values.Count < 2)
                return 0;

            double mean = values.Average();
            double sumOfSquaredDifferences = values.Sum(v => Math.Pow(v - mean, 2));
            return Math.Sqrt(sumOfSquaredDifferences / (values.Count - 1));
        }
    }

    /// <summary>
    /// Metric statistics data
    /// </summary>
    public class MetricStatistics
    {
        public string MetricName { get; set; }
        public double CurrentValue { get; set; }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public double AverageValue { get; set; }
        public double MedianValue { get; set; }
        public double StandardDeviation { get; set; }
        public int DataPointCount { get; set; }
        public double TimeRangeHours { get; set; }
    }

    /// <summary>
    /// Trend analysis results
    /// </summary>
    public class TrendAnalysis
    {
        public string MetricName { get; set; }
        public double PreviousPeriodAverage { get; set; }
        public double CurrentPeriodAverage { get; set; }
        public double PercentChange { get; set; }
        public string Trend { get; set; } // Up, Down, Stable
    }
}
