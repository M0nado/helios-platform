using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HELIOS.Platform.Core.Observability.Interfaces;

namespace HELIOS.Platform.Core.Observability.Services;

/// <summary>
/// OpenTelemetry tracing implementation.
/// Manages spans, traces, and distributed tracing across services.
/// </summary>
public class OpenTelemetryTracer : IOpenTelemetry
{
    private readonly ILogger<OpenTelemetryTracer> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Dictionary<string, TraceData> _traces = new();
    private readonly Dictionary<string, SpanData> _spans = new();
    private Interfaces.SamplingConfig _samplingConfig = new()
    {
        Strategy = Interfaces.SamplingStrategy.ParentBasedAlwaysOn,
        SamplingRatio = 1.0,
        RecordExceptions = true,
        RecordEvents = true
    };

    public OpenTelemetryTracer(ILogger<OpenTelemetryTracer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("OpenTelemetry Tracer initialized");
    }

    public ITraceSpan StartSpan(string operationName, Dictionary<string, object>? attributes = null)
    {
        var traceId = Guid.NewGuid().ToString();
        var spanId = Guid.NewGuid().ToString();

        var span = new TraceSpanImpl
        {
            SpanId = spanId,
            TraceId = traceId,
            OperationName = operationName,
            StartTime = DateTime.UtcNow,
            Attributes = attributes ?? new()
        };

        _semaphore.Wait();
        try
        {
            _spans[spanId] = new SpanData
            {
                SpanId = spanId,
                TraceId = traceId,
                OperationName = operationName,
                StartTime = DateTime.UtcNow,
                Attributes = attributes ?? new()
            };

            if (!_traces.ContainsKey(traceId))
            {
                _traces[traceId] = new TraceData { TraceId = traceId, StartTime = DateTime.UtcNow };
            }

            _logger.LogDebug("Span started: {OperationName} ({SpanId})", operationName, spanId);
        }
        finally
        {
            _semaphore.Release();
        }

        return span;
    }

    public void RecordEvent(ITraceSpan span, string eventName, Dictionary<string, object>? attributes = null)
    {
        if (span == null)
            throw new ArgumentNullException(nameof(span));

        _semaphore.Wait();
        try
        {
            if (_spans.TryGetValue(span.SpanId, out var spanData))
            {
                spanData.Events.Add(new Interfaces.SpanEvent
                {
                    Name = eventName,
                    Timestamp = DateTime.UtcNow,
                    Attributes = attributes ?? new()
                });
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void SetSpanStatus(ITraceSpan span, Interfaces.SpanStatus status, string? description = null)
    {
        if (span == null)
            throw new ArgumentNullException(nameof(span));

        _semaphore.Wait();
        try
        {
            if (_spans.TryGetValue(span.SpanId, out var spanData))
            {
                spanData.Status = status;
                if (!string.IsNullOrEmpty(description))
                    spanData.StatusDescription = description;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void RecordException(ITraceSpan span, Exception ex)
    {
        if (span == null || ex == null)
            return;

        SetSpanStatus(span, Interfaces.SpanStatus.Error, ex.Message);
        RecordEvent(span, "exception", new Dictionary<string, object>
        {
            { "exception.type", ex.GetType().Name },
            { "exception.message", ex.Message },
            { "exception.stacktrace", ex.StackTrace ?? "" }
        });
    }

    public void RecordMetric(string instrumentName, double value, string unit = "")
    {
        _logger.LogDebug("Metric recorded: {Instrument}={Value} {Unit}", instrumentName, value, unit);
    }

    public async Task<Interfaces.TraceSummary> GetTraceAsync(string traceId)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!_traces.TryGetValue(traceId, out var trace))
                return new Interfaces.TraceSummary();

            var spans = _spans.Values
                .Where(s => s.TraceId == traceId)
                .Select(s => new Interfaces.SpanDetail
                {
                    SpanId = s.SpanId,
                    OperationName = s.OperationName,
                    StartTime = s.StartTime,
                    Duration = s.EndTime.HasValue ? s.EndTime.Value - s.StartTime : TimeSpan.Zero,
                    Status = s.Status,
                    Attributes = s.Attributes,
                    Events = s.Events
                })
                .ToList();

            return new Interfaces.TraceSummary
            {
                TraceId = traceId,
                StartTime = trace.StartTime,
                Duration = DateTime.UtcNow - trace.StartTime,
                Spans = spans,
                SpanCount = spans.Count
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<List<Interfaces.TraceSummary>> QueryTracesAsync(Interfaces.TraceFilter filter)
    {
        await _semaphore.WaitAsync();
        try
        {
            var results = new List<Interfaces.TraceSummary>();

            foreach (var (traceId, trace) in _traces)
            {
                var spans = _spans.Values.Where(s => s.TraceId == traceId).ToList();

                if (!spans.Any())
                    continue;

                if (filter.OperationName != null && !spans.Any(s => s.OperationName.Contains(filter.OperationName)))
                    continue;

                var duration = DateTime.UtcNow - trace.StartTime;
                if (filter.MinDuration.HasValue && duration < filter.MinDuration.Value)
                    continue;
                if (filter.MaxDuration.HasValue && duration > filter.MaxDuration.Value)
                    continue;

                var spanDetails = spans.Select(s => new Interfaces.SpanDetail
                {
                    SpanId = s.SpanId,
                    OperationName = s.OperationName,
                    StartTime = s.StartTime,
                    Duration = s.EndTime.HasValue ? s.EndTime.Value - s.StartTime : TimeSpan.Zero,
                    Status = s.Status
                }).ToList();

                results.Add(new Interfaces.TraceSummary
                {
                    TraceId = traceId,
                    StartTime = trace.StartTime,
                    Duration = duration,
                    Spans = spanDetails,
                    SpanCount = spanDetails.Count
                });

                if (filter.Limit.HasValue && results.Count >= filter.Limit.Value)
                    break;
            }

            return results;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> ExportTracesAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            _logger.LogInformation("Exporting {TraceCount} traces", _traces.Count);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Interfaces.SamplingConfig GetSamplingConfig()
    {
        return _samplingConfig;
    }

    public void UpdateSamplingStrategy(Interfaces.SamplingStrategy strategy, double ratio)
    {
        _samplingConfig.Strategy = strategy;
        _samplingConfig.SamplingRatio = Math.Max(0, Math.Min(1, ratio));
        _logger.LogInformation("Sampling strategy updated: {Strategy} @ {Ratio}", strategy, ratio);
    }

    public async Task<Interfaces.TraceStatistics> GetStatisticsAsync(TimeSpan? period = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            var allSpans = _spans.Values.ToList();
            var durations = allSpans
                .Where(s => s.EndTime.HasValue)
                .Select(s => (s.EndTime.Value - s.StartTime).TotalMilliseconds)
                .ToList();

            return new Interfaces.TraceStatistics
            {
                TotalTraces = _traces.Count,
                ErrorTraces = _spans.Values.Count(s => s.Status == Interfaces.SpanStatus.Error),
                AverageDuration = durations.Any()
                    ? TimeSpan.FromMilliseconds(durations.Average())
                    : TimeSpan.Zero,
                TotalSpans = allSpans.Count,
                ErrorRate = allSpans.Any() ? (double)allSpans.Count(s => s.Status == Interfaces.SpanStatus.Error) / allSpans.Count : 0
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private class TraceSpanImpl : ITraceSpan
    {
        public string SpanId { get; set; } = string.Empty;
        public string TraceId { get; set; } = string.Empty;
        public string OperationName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public Dictionary<string, object> Attributes { get; set; } = new();

        public void SetAttribute(string key, object value)
        {
            Attributes[key] = value;
        }

        public void Dispose()
        {
        }
    }

    private class TraceData
    {
        public string TraceId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
    }

    private class SpanData
    {
        public string SpanId { get; set; } = string.Empty;
        public string TraceId { get; set; } = string.Empty;
        public string OperationName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public Interfaces.SpanStatus Status { get; set; } = Interfaces.SpanStatus.Unset;
        public string? StatusDescription { get; set; }
        public Dictionary<string, object> Attributes { get; set; } = new();
        public List<Interfaces.SpanEvent> Events { get; set; } = new();
    }
}

