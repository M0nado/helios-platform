using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MonadoBlade.Monitoring
{
    /// <summary>
    /// Interface for metrics collection
    /// </summary>
    public interface IMetricsCollector
    {
        void IncrementCounter(string name, long value = 1, Dictionary<string, string> tags = null);
        void RecordGauge(string name, double value, Dictionary<string, string> tags = null);
        void RecordHistogram(string name, double value, Dictionary<string, string> tags = null);
        Dictionary<string, object> GetMetrics();
        string ExportPrometheus();
        void ResetMetrics();
    }

    /// <summary>
    /// Advanced metrics collector with W3C trace context support and anomaly detection hooks
    /// </summary>
    public class AdvancedMetricsCollector : IMetricsCollector
    {
        private readonly ConcurrentDictionary<string, Counter> _counters = new();
        private readonly ConcurrentDictionary<string, Gauge> _gauges = new();
        private readonly ConcurrentDictionary<string, Histogram> _histograms = new();
        private readonly ReaderWriterLockSlim _metricsLock = new();
        private readonly object _traceContextLock = new();
        private readonly IAnomalyDetectionHooks _anomalyHooks;
        
        private string _currentTraceParent = "";
        private string _currentTraceState = "";

        public AdvancedMetricsCollector(IAnomalyDetectionHooks anomalyHooks = null)
        {
            _anomalyHooks = anomalyHooks;
        }

        /// <summary>
        /// Set W3C Trace Context (traceparent header format)
        /// Format: version-traceId-parentId-traceFlags
        /// </summary>
        public void SetTraceContext(string traceparent, string tracestate = "")
        {
            lock (_traceContextLock)
            {
                _currentTraceParent = traceparent ?? "";
                _currentTraceState = tracestate ?? "";
            }
        }

        /// <summary>
        /// Get current W3C Trace Context
        /// </summary>
        public (string TraceParent, string TraceState) GetTraceContext()
        {
            lock (_traceContextLock)
            {
                return (_currentTraceParent, _currentTraceState);
            }
        }

        /// <summary>
        /// Increment a counter metric with optional tags
        /// </summary>
        public void IncrementCounter(string name, long value = 1, Dictionary<string, string> tags = null)
        {
            var key = BuildMetricKey(name, tags);
            
            _counters.AddOrUpdate(key, 
                new Counter { Name = name, Value = value, Tags = tags ?? new(), Timestamp = DateTime.UtcNow },
                (k, existing) =>
                {
                    existing.Value += value;
                    existing.Timestamp = DateTime.UtcNow;
                    return existing;
                });

            CheckAnomalies(name, (double)value, "counter", tags);
        }

        /// <summary>
        /// Record a gauge metric (instantaneous value)
        /// </summary>
        public void RecordGauge(string name, double value, Dictionary<string, string> tags = null)
        {
            var key = BuildMetricKey(name, tags);
            var gauge = new Gauge 
            { 
                Name = name, 
                Value = value, 
                Tags = tags ?? new(),
                Timestamp = DateTime.UtcNow 
            };
            
            _gauges[key] = gauge;
            CheckAnomalies(name, value, "gauge", tags);
        }

        /// <summary>
        /// Record a histogram value for distribution analysis
        /// </summary>
        public void RecordHistogram(string name, double value, Dictionary<string, string> tags = null)
        {
            var key = BuildMetricKey(name, tags);
            
            if (!_histograms.TryGetValue(key, out var histogram))
            {
                histogram = new Histogram 
                { 
                    Name = name, 
                    Tags = tags ?? new(),
                    Values = new List<double>(),
                    CreatedAt = DateTime.UtcNow
                };
                _histograms[key] = histogram;
            }

            histogram.Values.Add(value);
            histogram.LastUpdated = DateTime.UtcNow;
            CheckAnomalies(name, value, "histogram", tags);
        }

        /// <summary>
        /// Get latency percentile (50th, 95th, 99th)
        /// </summary>
        public double GetLatencyPercentile(string name, double percentile)
        {
            var key = _histograms.Keys.FirstOrDefault(k => k.StartsWith(name));
            if (key != null && _histograms.TryGetValue(key, out var histogram))
            {
                var sorted = histogram.Values.OrderBy(v => v).ToList();
                var index = (int)Math.Ceiling((percentile / 100.0) * sorted.Count) - 1;
                return index >= 0 && index < sorted.Count ? sorted[index] : 0;
            }
            return 0;
        }

        /// <summary>
        /// Get error rate for a metric group
        /// </summary>
        public double GetErrorRate(string metricPrefix)
        {
            var totalRequests = _counters
                .Where(kvp => kvp.Key.StartsWith(metricPrefix) && kvp.Key.Contains("requests"))
                .Sum(kvp => kvp.Value.Value);

            var errors = _counters
                .Where(kvp => kvp.Key.StartsWith(metricPrefix) && kvp.Key.Contains("errors"))
                .Sum(kvp => kvp.Value.Value);

            return totalRequests > 0 ? (errors / (double)totalRequests) * 100.0 : 0;
        }

        /// <summary>
        /// Get all metrics as dictionary
        /// </summary>
        public Dictionary<string, object> GetMetrics()
        {
            _metricsLock.EnterReadLock();
            try
            {
                var metrics = new Dictionary<string, object>();
                
                foreach (var counter in _counters)
                {
                    metrics[$"counter_{counter.Value.Name}"] = counter.Value.Value;
                }

                foreach (var gauge in _gauges)
                {
                    metrics[$"gauge_{gauge.Value.Name}"] = gauge.Value.Value;
                }

                foreach (var histogram in _histograms)
                {
                    metrics[$"histogram_{histogram.Value.Name}_count"] = histogram.Value.Values.Count;
                    metrics[$"histogram_{histogram.Value.Name}_sum"] = histogram.Value.Values.Sum();
                    metrics[$"histogram_{histogram.Value.Name}_mean"] = histogram.Value.Values.Any() 
                        ? histogram.Value.Values.Average() 
                        : 0;
                    metrics[$"histogram_{histogram.Value.Name}_min"] = histogram.Value.Values.Any() 
                        ? histogram.Value.Values.Min() 
                        : 0;
                    metrics[$"histogram_{histogram.Value.Name}_max"] = histogram.Value.Values.Any() 
                        ? histogram.Value.Values.Max() 
                        : 0;
                    metrics[$"histogram_{histogram.Value.Name}_p95"] = GetLatencyPercentile(histogram.Value.Name, 95);
                    metrics[$"histogram_{histogram.Value.Name}_p99"] = GetLatencyPercentile(histogram.Value.Name, 99);
                }

                return metrics;
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Export metrics in Prometheus format
        /// </summary>
        public string ExportPrometheus()
        {
            _metricsLock.EnterReadLock();
            try
            {
                var sb = new StringBuilder();
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                foreach (var counter in _counters)
                {
                    var tagsStr = FormatTagsPrometheus(counter.Value.Tags);
                    sb.AppendLine($"# TYPE {counter.Value.Name} counter");
                    sb.AppendLine($"{counter.Value.Name}{tagsStr} {counter.Value.Value} {timestamp}");
                }

                foreach (var gauge in _gauges)
                {
                    var tagsStr = FormatTagsPrometheus(gauge.Value.Tags);
                    sb.AppendLine($"# TYPE {gauge.Value.Name} gauge");
                    sb.AppendLine($"{gauge.Value.Name}{tagsStr} {gauge.Value.Value} {timestamp}");
                }

                foreach (var histogram in _histograms)
                {
                    var tagsStr = FormatTagsPrometheus(histogram.Value.Tags);
                    sb.AppendLine($"# TYPE {histogram.Value.Name} histogram");
                    
                    if (histogram.Value.Values.Any())
                    {
                        sb.AppendLine($"{histogram.Value.Name}_bucket{{le=\"0.1\"{tagsStr}}} {histogram.Value.Values.Count(v => v <= 0.1)} {timestamp}");
                        sb.AppendLine($"{histogram.Value.Name}_bucket{{le=\"0.5\"{tagsStr}}} {histogram.Value.Values.Count(v => v <= 0.5)} {timestamp}");
                        sb.AppendLine($"{histogram.Value.Name}_bucket{{le=\"1.0\"{tagsStr}}} {histogram.Value.Values.Count(v => v <= 1.0)} {timestamp}");
                        sb.AppendLine($"{histogram.Value.Name}_bucket{{le=\"+Inf\"{tagsStr}}} {histogram.Value.Values.Count} {timestamp}");
                    }
                    
                    sb.AppendLine($"{histogram.Value.Name}_count{tagsStr} {histogram.Value.Values.Count} {timestamp}");
                    sb.AppendLine($"{histogram.Value.Name}_sum{tagsStr} {(histogram.Value.Values.Any() ? histogram.Value.Values.Sum() : 0)} {timestamp}");
                }

                return sb.ToString();
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Reset all metrics
        /// </summary>
        public void ResetMetrics()
        {
            _metricsLock.EnterWriteLock();
            try
            {
                _counters.Clear();
                _gauges.Clear();
                _histograms.Clear();
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }

        private void CheckAnomalies(string metricName, double value, string metricType, Dictionary<string, string> tags)
        {
            if (_anomalyHooks != null)
            {
                try
                {
                    _anomalyHooks.CheckMetricAnomaly(metricName, value, metricType, tags);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Anomaly detection error: {ex.Message}");
                }
            }
        }

        private string BuildMetricKey(string name, Dictionary<string, string> tags)
        {
            if (tags == null || tags.Count == 0)
                return name;

            var tagStr = string.Join(",", tags.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            return $"{name}_{{{tagStr}}}";
        }

        private string FormatTagsPrometheus(Dictionary<string, string> tags)
        {
            if (tags == null || tags.Count == 0)
                return "";

            var tagStr = string.Join(",", tags.Select(kvp => $"{kvp.Key}=\"{kvp.Value}\""));
            return $"{{{tagStr}}}";
        }

        private class Counter
        {
            public string Name { get; set; }
            public long Value { get; set; }
            public Dictionary<string, string> Tags { get; set; }
            public DateTime Timestamp { get; set; }
        }

        private class Gauge
        {
            public string Name { get; set; }
            public double Value { get; set; }
            public Dictionary<string, string> Tags { get; set; }
            public DateTime Timestamp { get; set; }
        }

        private class Histogram
        {
            public string Name { get; set; }
            public Dictionary<string, string> Tags { get; set; }
            public List<double> Values { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime LastUpdated { get; set; }
        }
    }
}
