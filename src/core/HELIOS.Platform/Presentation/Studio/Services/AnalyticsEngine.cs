using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HELIOS.Platform.Presentation.Studio.Models;

namespace HELIOS.Platform.Presentation.Studio.Services
{
    /// <summary>
    /// Analytics engine for performance analysis and insights
    /// Tracks metrics history and provides aggregated analytics
    /// </summary>
    public class AnalyticsEngine
    {
        private readonly List<AnalyticsDataPoint> _dataPoints;
        private readonly Dictionary<string, MetricTrendData> _trends;
        private readonly int _maxDataPoints = 100000;

        public AnalyticsEngine()
        {
            _dataPoints = new List<AnalyticsDataPoint>();
            _trends = new Dictionary<string, MetricTrendData>();
        }

        /// <summary>
        /// Record a metric data point
        /// </summary>
        public void RecordMetric(string metricName, double value, Dictionary<string, string> tags = null)
        {
            var dataPoint = new AnalyticsDataPoint
            {
                Id = Guid.NewGuid().ToString("N"),
                Timestamp = DateTime.UtcNow,
                MetricName = metricName,
                Value = value,
                Tags = tags ?? new Dictionary<string, string>()
            };

            _dataPoints.Add(dataPoint);

            // Maintain size limit
            if (_dataPoints.Count > _maxDataPoints)
                _dataPoints.RemoveRange(0, _dataPoints.Count - _maxDataPoints);

            // Update trend data
            UpdateTrendData(metricName, value);
        }

        /// <summary>
        /// Record metrics snapshot
        /// </summary>
        public void RecordSnapshot(SystemMetrics metrics)
        {
            RecordMetric("CPU", metrics.CpuUsagePercent);
            RecordMetric("Memory", metrics.MemoryUsagePercent);
            RecordMetric("Disk", metrics.DiskUsagePercent);
            RecordMetric("Network", metrics.NetworkBytesPerSecond);
            if (metrics.GpuUsagePercent > 0)
                RecordMetric("GPU", metrics.GpuUsagePercent);
        }

        /// <summary>
        /// Get analytics for a metric over a time period
        /// </summary>
        public AnalyticsResult GetAnalytics(string metricName, TimeSpan period)
        {
            var cutoff = DateTime.UtcNow.Subtract(period);
            var data = _dataPoints
                .Where(p => p.MetricName == metricName && p.Timestamp >= cutoff)
                .ToList();

            if (!data.Any())
                return null;

            var values = data.Select(d => d.Value).ToList();
            var percentiles = CalculatePercentiles(values);

            return new AnalyticsResult
            {
                MetricName = metricName,
                Period = period,
                Count = data.Count,
                Min = values.Min(),
                Max = values.Max(),
                Mean = values.Average(),
                Median = GetMedian(values),
                StandardDeviation = CalculateStandardDeviation(values),
                P25 = percentiles["P25"],
                P50 = percentiles["P50"],
                P75 = percentiles["P75"],
                P95 = percentiles["P95"],
                P99 = percentiles["P99"],
                Percentiles = percentiles
            };
        }

        /// <summary>
        /// Get peak usage times for a metric
        /// </summary>
        public PeakAnalysis GetPeakUsage(string metricName, int topN = 10)
        {
            var data = _dataPoints
                .Where(p => p.MetricName == metricName)
                .OrderByDescending(p => p.Value)
                .Take(topN * 3)
                .ToList();

            if (!data.Any())
                return null;

            var peaks = data
                .Take(topN)
                .GroupBy(d => d.Timestamp.Hour)
                .Select(g => new HourlyPeak
                {
                    Hour = g.Key,
                    AverageValue = g.Average(d => d.Value),
                    MaxValue = g.Max(d => d.Value),
                    Count = g.Count()
                })
                .OrderByDescending(p => p.AverageValue)
                .ToList();

            return new PeakAnalysis
            {
                MetricName = metricName,
                Peaks = peaks,
                TotalPeaks = peaks.Count,
                AveragePeakValue = peaks.Average(p => p.AverageValue)
            };
        }

        /// <summary>
        /// Analyze metric correlations
        /// </summary>
        public CorrelationAnalysis AnalyzeCorrelation(string metric1, string metric2, TimeSpan period)
        {
            var cutoff = DateTime.UtcNow.Subtract(period);
            var data1 = _dataPoints
                .Where(p => p.MetricName == metric1 && p.Timestamp >= cutoff)
                .OrderBy(p => p.Timestamp)
                .ToList();

            var data2 = _dataPoints
                .Where(p => p.MetricName == metric2 && p.Timestamp >= cutoff)
                .OrderBy(p => p.Timestamp)
                .ToList();

            if (data1.Count < 2 || data2.Count < 2)
                return null;

            var correlation = CalculatePearsonCorrelation(
                data1.Select(d => d.Value).ToList(),
                data2.Select(d => d.Value).ToList()
            );

            return new CorrelationAnalysis
            {
                Metric1 = metric1,
                Metric2 = metric2,
                Correlation = correlation,
                Strength = GetCorrelationStrength(correlation)
            };
        }

        /// <summary>
        /// Get metric summary for dashboard
        /// </summary>
        public MetricSummary GetMetricSummary(string metricName)
        {
            if (!_trends.TryGetValue(metricName, out var trend))
                return null;

            return new MetricSummary
            {
                MetricName = metricName,
                CurrentValue = trend.CurrentValue,
                Previous1HourValue = trend.OneHourAgoValue,
                Previous24HourValue = trend.OneDayAgoValue,
                TrendChange1Hour = trend.CurrentValue - trend.OneHourAgoValue,
                TrendChange24Hour = trend.CurrentValue - trend.OneDayAgoValue,
                TrendDirection1Hour = GetTrendDirection(trend.CurrentValue - trend.OneHourAgoValue),
                TrendDirection24Hour = GetTrendDirection(trend.CurrentValue - trend.OneDayAgoValue)
            };
        }

        /// <summary>
        /// Get all metric summaries
        /// </summary>
        public IEnumerable<MetricSummary> GetAllMetricSummaries()
        {
            return _trends.Values.Select(t => new MetricSummary
            {
                MetricName = t.MetricName,
                CurrentValue = t.CurrentValue,
                Previous1HourValue = t.OneHourAgoValue,
                Previous24HourValue = t.OneDayAgoValue,
                TrendChange1Hour = t.CurrentValue - t.OneHourAgoValue,
                TrendChange24Hour = t.CurrentValue - t.OneDayAgoValue,
                TrendDirection1Hour = GetTrendDirection(t.CurrentValue - t.OneHourAgoValue),
                TrendDirection24Hour = GetTrendDirection(t.CurrentValue - t.OneDayAgoValue)
            });
        }

        /// <summary>
        /// Clear data older than specified days
        /// </summary>
        public void ClearOldData(int daysToKeep = 30)
        {
            var cutoff = DateTime.UtcNow.AddDays(-daysToKeep);
            _dataPoints.RemoveAll(p => p.Timestamp < cutoff);
        }

        private void UpdateTrendData(string metricName, double value)
        {
            if (!_trends.ContainsKey(metricName))
            {
                _trends[metricName] = new MetricTrendData
                {
                    MetricName = metricName,
                    CurrentValue = value,
                    OneHourAgoValue = value,
                    OneDayAgoValue = value,
                    LastUpdated = DateTime.UtcNow
                };
            }
            else
            {
                var trend = _trends[metricName];

                // Check if we need to update historical values
                var hourAgo = DateTime.UtcNow.AddHours(-1);
                var dayAgo = DateTime.UtcNow.AddDays(-1);

                if (trend.LastUpdated < hourAgo)
                    trend.OneHourAgoValue = trend.CurrentValue;

                if (trend.LastUpdated < dayAgo)
                    trend.OneDayAgoValue = trend.CurrentValue;

                trend.CurrentValue = value;
                trend.LastUpdated = DateTime.UtcNow;
            }
        }

        private double CalculatePearsonCorrelation(List<double> x, List<double> y)
        {
            if (x.Count != y.Count || x.Count < 2)
                return 0;

            double meanX = x.Average();
            double meanY = y.Average();

            double sumXY = 0;
            double sumX2 = 0;
            double sumY2 = 0;

            for (int i = 0; i < x.Count; i++)
            {
                double dx = x[i] - meanX;
                double dy = y[i] - meanY;
                sumXY += dx * dy;
                sumX2 += dx * dx;
                sumY2 += dy * dy;
            }

            double denominator = Math.Sqrt(sumX2 * sumY2);
            if (denominator == 0)
                return 0;

            return sumXY / denominator;
        }

        private Dictionary<string, double> CalculatePercentiles(List<double> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            var percentiles = new Dictionary<string, double>();

            percentiles["P25"] = GetPercentile(sorted, 0.25);
            percentiles["P50"] = GetPercentile(sorted, 0.50);
            percentiles["P75"] = GetPercentile(sorted, 0.75);
            percentiles["P95"] = GetPercentile(sorted, 0.95);
            percentiles["P99"] = GetPercentile(sorted, 0.99);

            return percentiles;
        }

        private double GetPercentile(List<double> sorted, double percentile)
        {
            if (sorted.Count == 0)
                return 0;

            int index = (int)Math.Ceiling(percentile * sorted.Count) - 1;
            return index >= 0 && index < sorted.Count ? sorted[index] : 0;
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

        private string GetCorrelationStrength(double correlation)
        {
            double absCorr = Math.Abs(correlation);
            if (absCorr >= 0.7)
                return "Strong";
            if (absCorr >= 0.4)
                return "Moderate";
            if (absCorr >= 0.2)
                return "Weak";
            return "Very Weak";
        }

        private string GetTrendDirection(double change)
        {
            if (change > 0.5)
                return "Up";
            if (change < -0.5)
                return "Down";
            return "Stable";
        }
    }

    /// <summary>
    /// Analytics data point
    /// </summary>
    public class AnalyticsDataPoint
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string MetricName { get; set; }
        public double Value { get; set; }
        public Dictionary<string, string> Tags { get; set; }
    }

    /// <summary>
    /// Analytics result with statistics
    /// </summary>
    public class AnalyticsResult
    {
        public string MetricName { get; set; }
        public TimeSpan Period { get; set; }
        public int Count { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double Mean { get; set; }
        public double Median { get; set; }
        public double StandardDeviation { get; set; }
        public double P25 { get; set; }
        public double P50 { get; set; }
        public double P75 { get; set; }
        public double P95 { get; set; }
        public double P99 { get; set; }
        public Dictionary<string, double> Percentiles { get; set; }
    }

    /// <summary>
    /// Peak usage analysis
    /// </summary>
    public class PeakAnalysis
    {
        public string MetricName { get; set; }
        public List<HourlyPeak> Peaks { get; set; }
        public int TotalPeaks { get; set; }
        public double AveragePeakValue { get; set; }
    }

    /// <summary>
    /// Hourly peak data
    /// </summary>
    public class HourlyPeak
    {
        public int Hour { get; set; }
        public double AverageValue { get; set; }
        public double MaxValue { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// Correlation analysis between two metrics
    /// </summary>
    public class CorrelationAnalysis
    {
        public string Metric1 { get; set; }
        public string Metric2 { get; set; }
        public double Correlation { get; set; }
        public string Strength { get; set; }
    }

    /// <summary>
    /// Metric summary for dashboard
    /// </summary>
    public class MetricSummary
    {
        public string MetricName { get; set; }
        public double CurrentValue { get; set; }
        public double Previous1HourValue { get; set; }
        public double Previous24HourValue { get; set; }
        public double TrendChange1Hour { get; set; }
        public double TrendChange24Hour { get; set; }
        public string TrendDirection1Hour { get; set; }
        public string TrendDirection24Hour { get; set; }
    }

    /// <summary>
    /// Metric trend data
    /// </summary>
    internal class MetricTrendData
    {
        public string MetricName { get; set; }
        public double CurrentValue { get; set; }
        public double OneHourAgoValue { get; set; }
        public double OneDayAgoValue { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
