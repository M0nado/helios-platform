using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MonadoBlade.Core.Observability
{
    /// <summary>
    /// STREAM F: Advanced Metrics & Observability - Distributed tracing, metrics collection, anomaly detection
    /// Expected: Complete system visibility
    /// </summary>
    public interface IMetricsCollector
    {
        void RecordMetric(string name, double value, Dictionary<string, string> tags = null);
        void RecordDuration(string operationName, long durationMs, Dictionary<string, string> tags = null);
        MetricSnapshot GetSnapshot(string metricName);
    }

    public class MetricSnapshot
    {
        public string Name { get; set; }
        public double Value { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double Average { get; set; }
        public long Count { get; set; }
        public DateTime CollectedAt { get; set; }
    }

    public class AdvancedMetricsCollector : IMetricsCollector
    {
        private readonly ConcurrentDictionary<string, MetricBuffer> _metrics = new();
        private readonly int _bufferSize = 1000;

        private class MetricBuffer
        {
            public string Name { get; set; }
            public ConcurrentQueue<MetricValue> Values { get; set; } = new();
            public double Min { get; set; } = double.MaxValue;
            public double Max { get; set; } = double.MinValue;
            public long Count { get; set; } = 0;
            public object Lock { get; } = new();
        }

        private class MetricValue
        {
            public double Value { get; set; }
            public DateTime Timestamp { get; set; }
            public Dictionary<string, string> Tags { get; set; }
        }

        public void RecordMetric(string name, double value, Dictionary<string, string> tags = null)
        {
            var buffer = _metrics.GetOrAdd(name, _ => new MetricBuffer { Name = name });

            lock (buffer.Lock)
            {
                buffer.Values.Enqueue(new MetricValue
                {
                    Value = value,
                    Timestamp = DateTime.UtcNow,
                    Tags = tags ?? new()
                });

                buffer.Min = Math.Min(buffer.Min, value);
                buffer.Max = Math.Max(buffer.Max, value);
                buffer.Count++;

                // Maintain buffer size
                while (buffer.Values.Count > _bufferSize)
                {
                    buffer.Values.TryDequeue(out _);
                }
            }
        }

        public void RecordDuration(string operationName, long durationMs, Dictionary<string, string> tags = null)
        {
            RecordMetric($"{operationName}.duration_ms", durationMs, tags);
        }

        public MetricSnapshot GetSnapshot(string metricName)
        {
            if (!_metrics.TryGetValue(metricName, out var buffer))
                return null;

            lock (buffer.Lock)
            {
                var values = buffer.Values.Select(v => v.Value).ToList();
                var avg = values.Count > 0 ? values.Average() : 0;

                return new MetricSnapshot
                {
                    Name = metricName,
                    Value = values.LastOrDefault(),
                    Min = buffer.Min == double.MaxValue ? 0 : buffer.Min,
                    Max = buffer.Max == double.MinValue ? 0 : buffer.Max,
                    Average = avg,
                    Count = buffer.Count,
                    CollectedAt = DateTime.UtcNow
                };
            }
        }

        public List<MetricSnapshot> GetAllSnapshots()
        {
            return _metrics.Values
                .Select(b => GetSnapshot(b.Name))
                .Where(s => s != null)
                .ToList();
        }
    }

    public class DistributedTraceCollector
    {
        private readonly ConcurrentDictionary<string, TraceContext> _traces = new();
        private readonly string _serviceId;

        public class TraceContext
        {
            public string TraceId { get; set; }
            public string SpanId { get; set; }
            public string ParentSpanId { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime? EndTime { get; set; }
            public string OperationName { get; set; }
            public List<Span> Spans { get; set; } = new();
            public Dictionary<string, string> Tags { get; set; } = new();
        }

        public class Span
        {
            public string SpanId { get; set; }
            public string OperationName { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime? EndTime { get; set; }
            public Dictionary<string, object> Logs { get; set; } = new();
            public Dictionary<string, string> Tags { get; set; } = new();
        }

        public DistributedTraceCollector(string serviceId = "MonadoBlade")
        {
            _serviceId = serviceId;
        }

        public string StartTrace(string operationName)
        {
            var traceId = $"{_serviceId}-{Guid.NewGuid().ToString().Substring(0, 8)}";
            var context = new TraceContext
            {
                TraceId = traceId,
                SpanId = Guid.NewGuid().ToString().Substring(0, 8),
                OperationName = operationName,
                StartTime = DateTime.UtcNow
            };
            _traces[traceId] = context;
            return traceId;
        }

        public void EndTrace(string traceId)
        {
            if (_traces.TryGetValue(traceId, out var context))
            {
                context.EndTime = DateTime.UtcNow;
            }
        }

        public void AddSpan(string traceId, string operationName)
        {
            if (_traces.TryGetValue(traceId, out var context))
            {
                var span = new Span
                {
                    SpanId = Guid.NewGuid().ToString().Substring(0, 8),
                    OperationName = operationName,
                    StartTime = DateTime.UtcNow
                };
                context.Spans.Add(span);
            }
        }

        public void EndSpan(string traceId, string operationName)
        {
            if (_traces.TryGetValue(traceId, out var context))
            {
                var span = context.Spans.FirstOrDefault(s => s.OperationName == operationName && s.EndTime == null);
                if (span != null)
                {
                    span.EndTime = DateTime.UtcNow;
                }
            }
        }

        public void AddLogToTrace(string traceId, string level, string message, Dictionary<string, object> fields = null)
        {
            if (_traces.TryGetValue(traceId, out var context))
            {
                var lastSpan = context.Spans.LastOrDefault();
                if (lastSpan != null)
                {
                    lastSpan.Logs[$"{DateTime.UtcNow.Ticks}:{level}"] = message;
                }
            }
        }

        public TraceContext GetTrace(string traceId)
        {
            _traces.TryGetValue(traceId, out var context);
            return context;
        }

        public List<TraceContext> GetCompletedTraces(int limit = 100)
        {
            return _traces.Values
                .Where(t => t.EndTime.HasValue)
                .OrderByDescending(t => t.EndTime)
                .Take(limit)
                .ToList();
        }
    }

    public class AnomalyDetector
    {
        private readonly Dictionary<string, StatisticalModel> _models = new();
        private readonly object _modelLock = new();

        public class StatisticalModel
        {
            public string MetricName { get; set; }
            public double Mean { get; set; }
            public double StdDev { get; set; }
            public int Samples { get; set; }
            public List<double> RecentValues { get; set; } = new();
        }

        public class AnomalyResult
        {
            public bool IsAnomaly { get; set; }
            public double ZScore { get; set; }
            public string Reason { get; set; }
        }

        public void UpdateModel(string metricName, double value)
        {
            lock (_modelLock)
            {
                if (!_models.TryGetValue(metricName, out var model))
                {
                    model = new StatisticalModel { MetricName = metricName };
                    _models[metricName] = model;
                }

                model.RecentValues.Add(value);
                if (model.RecentValues.Count > 100)
                    model.RecentValues.RemoveAt(0);

                // Update statistics
                model.Samples++;
                model.Mean = model.RecentValues.Average();
                var variance = model.RecentValues.Select(v => Math.Pow(v - model.Mean, 2)).Average();
                model.StdDev = Math.Sqrt(variance);
            }
        }

        public AnomalyResult DetectAnomaly(string metricName, double value, double thresholdSigma = 3.0)
        {
            lock (_modelLock)
            {
                if (!_models.TryGetValue(metricName, out var model) || model.Samples < 10)
                {
                    return new AnomalyResult { IsAnomaly = false, Reason = "Insufficient data" };
                }

                var zScore = model.StdDev > 0 ? (value - model.Mean) / model.StdDev : 0;
                var isAnomaly = Math.Abs(zScore) > thresholdSigma;

                return new AnomalyResult
                {
                    IsAnomaly = isAnomaly,
                    ZScore = zScore,
                    Reason = isAnomaly ? $"Value {zScore:F2}σ from mean" : "Normal"
                };
            }
        }
    }

    public class MetricsDashboard
    {
        private readonly IMetricsCollector _metricsCollector;
        private readonly DistributedTraceCollector _traceCollector;
        private readonly AnomalyDetector _anomalyDetector;
        private readonly Dictionary<string, DashboardWidget> _widgets = new();
        private readonly object _widgetLock = new();

        public class DashboardWidget
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string MetricName { get; set; }
            public VisualizationType VisualizationType { get; set; }
            public DateTime LastUpdated { get; set; }
            public Dictionary<string, object> Data { get; set; } = new();
        }

        public enum VisualizationType
        {
            Gauge,
            LineChart,
            BarChart,
            Heatmap,
            Table
        }

        public MetricsDashboard(IMetricsCollector metricsCollector, 
            DistributedTraceCollector traceCollector, AnomalyDetector anomalyDetector)
        {
            _metricsCollector = metricsCollector;
            _traceCollector = traceCollector;
            _anomalyDetector = anomalyDetector;
        }

        public void RegisterWidget(DashboardWidget widget)
        {
            lock (_widgetLock)
            {
                _widgets[widget.Id] = widget;
            }
        }

        public async Task RefreshDashboardAsync(CancellationToken ct = default)
        {
            List<DashboardWidget> widgetsToRefresh;
            lock (_widgetLock)
            {
                widgetsToRefresh = new(_widgets.Values);
            }

            var tasks = widgetsToRefresh.Select(w => RefreshWidgetAsync(w, ct));
            await Task.WhenAll(tasks);
        }

        private async Task RefreshWidgetAsync(DashboardWidget widget, CancellationToken ct)
        {
            var snapshot = _metricsCollector.GetSnapshot(widget.MetricName);
            if (snapshot != null)
            {
                widget.Data["current"] = snapshot.Value;
                widget.Data["average"] = snapshot.Average;
                widget.Data["min"] = snapshot.Min;
                widget.Data["max"] = snapshot.Max;
                widget.LastUpdated = DateTime.UtcNow;

                // Check for anomalies
                var anomaly = _anomalyDetector.DetectAnomaly(widget.MetricName, snapshot.Value);
                widget.Data["is_anomaly"] = anomaly.IsAnomaly;
                widget.Data["anomaly_reason"] = anomaly.Reason;
            }

            await Task.CompletedTask;
        }

        public DashboardSnapshot GetSnapshot()
        {
            lock (_widgetLock)
            {
                return new DashboardSnapshot
                {
                    Widgets = new Dictionary<string, Dictionary<string, object>>(_widgets
                        .ToDictionary(kv => kv.Key, kv => new Dictionary<string, object>(kv.Value.Data))),
                    CapturedAt = DateTime.UtcNow
                };
            }
        }
    }

    public class DashboardSnapshot
    {
        public Dictionary<string, Dictionary<string, object>> Widgets { get; set; }
        public DateTime CapturedAt { get; set; }
    }

    public class TrendAnalyzer
    {
        private readonly AdvancedMetricsCollector _metricsCollector;
        private readonly Dictionary<string, List<DataPoint>> _historicalData = new();
        private readonly object _dataLock = new();

        private class DataPoint
        {
            public double Value { get; set; }
            public DateTime Timestamp { get; set; }
        }

        public TrendAnalyzer(AdvancedMetricsCollector metricsCollector)
        {
            _metricsCollector = metricsCollector;
        }

        public void RecordDataPoint(string metricName, double value)
        {
            lock (_dataLock)
            {
                if (!_historicalData.ContainsKey(metricName))
                    _historicalData[metricName] = new();

                _historicalData[metricName].Add(new DataPoint
                {
                    Value = value,
                    Timestamp = DateTime.UtcNow
                });

                // Keep last 1000 points
                if (_historicalData[metricName].Count > 1000)
                    _historicalData[metricName].RemoveAt(0);
            }
        }

        public TrendDirection AnalyzeTrend(string metricName, int lookbackMinutes = 60)
        {
            lock (_dataLock)
            {
                if (!_historicalData.TryGetValue(metricName, out var data) || data.Count < 2)
                    return TrendDirection.Flat;

                var cutoff = DateTime.UtcNow.AddMinutes(-lookbackMinutes);
                var recentData = data.Where(d => d.Timestamp >= cutoff).ToList();

                if (recentData.Count < 2)
                    return TrendDirection.Flat;

                var firstHalf = recentData.Take(recentData.Count / 2).Average(d => d.Value);
                var secondHalf = recentData.Skip(recentData.Count / 2).Average(d => d.Value);

                var changePercent = (secondHalf - firstHalf) / firstHalf * 100;

                if (changePercent > 5)
                    return TrendDirection.Up;
                if (changePercent < -5)
                    return TrendDirection.Down;
                return TrendDirection.Flat;
            }
        }
    }

    public enum TrendDirection
    {
        Up,
        Down,
        Flat
    }
}
