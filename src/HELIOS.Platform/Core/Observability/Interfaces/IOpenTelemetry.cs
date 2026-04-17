using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Observability.Interfaces;

/// <summary>
/// OpenTelemetry instrumentation and tracing service.
/// Manages spans, traces, and distributed tracing across services.
/// </summary>
public interface IOpenTelemetry
{
    /// <summary>
    /// Start a new trace span for operation instrumentation.
    /// </summary>
    /// <param name="operationName">Name of the operation being traced</param>
    /// <param name="attributes">Optional span attributes</param>
    /// <returns>Span context for nested operations</returns>
    ITraceSpan StartSpan(string operationName, Dictionary<string, object>? attributes = null);

    /// <summary>
    /// Record a trace event within a span.
    /// </summary>
    void RecordEvent(ITraceSpan span, string eventName, Dictionary<string, object>? attributes = null);

    /// <summary>
    /// Set span status (Success, Error, Unset).
    /// </summary>
    void SetSpanStatus(ITraceSpan span, SpanStatus status, string? description = null);

    /// <summary>
    /// Add exception information to span.
    /// </summary>
    void RecordException(ITraceSpan span, Exception ex);

    /// <summary>
    /// Record custom metric via metrics API.
    /// </summary>
    void RecordMetric(string instrumentName, double value, string unit = "");

    /// <summary>
    /// Get trace summary for distributed tracing.
    /// </summary>
    /// <param name="traceId">Trace ID to retrieve</param>
    /// <returns>Trace with all spans</returns>
    Task<TraceSummary> GetTraceAsync(string traceId);

    /// <summary>
    /// Query traces with filtering.
    /// </summary>
    /// <param name="filter">Trace filter criteria</param>
    /// <returns>Matching traces</returns>
    Task<List<TraceSummary>> QueryTracesAsync(TraceFilter filter);

    /// <summary>
    /// Export traces to backend (Jaeger, DataDog, etc).
    /// </summary>
    Task<bool> ExportTracesAsync();

    /// <summary>
    /// Get sampler configuration.
    /// </summary>
    SamplingConfig GetSamplingConfig();

    /// <summary>
    /// Update sampling strategy.
    /// </summary>
    void UpdateSamplingStrategy(SamplingStrategy strategy, double ratio);

    /// <summary>
    /// Get trace statistics.
    /// </summary>
    Task<TraceStatistics> GetStatisticsAsync(TimeSpan? period = null);
}

/// <summary>
/// Active trace span.
/// </summary>
public interface ITraceSpan : IDisposable
{
    string SpanId { get; }
    string TraceId { get; }
    Dictionary<string, object> Attributes { get; }
    void SetAttribute(string key, object value);
}

/// <summary>
/// Trace span status.
/// </summary>
public enum SpanStatus
{
    Unset,
    Ok,
    Error
}

/// <summary>
/// Sampling strategy for traces.
/// </summary>
public enum SamplingStrategy
{
    AlwaysOn,
    AlwaysOff,
    ParentBasedAlwaysOn,
    ParentBasedAlwaysOff,
    TraceIdRatioBased,
    Dynamic
}

/// <summary>
/// Trace summary with all spans.
/// </summary>
public class TraceSummary
{
    public string TraceId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public TimeSpan Duration { get; set; }
    public List<SpanDetail> Spans { get; set; } = new();
    public string RootServiceName { get; set; } = string.Empty;
    public int SpanCount { get; set; }
}

/// <summary>
/// Individual span in trace.
/// </summary>
public class SpanDetail
{
    public string SpanId { get; set; } = string.Empty;
    public string ParentSpanId { get; set; } = string.Empty;
    public string OperationName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public TimeSpan Duration { get; set; }
    public SpanStatus Status { get; set; }
    public Dictionary<string, object> Attributes { get; set; } = new();
    public List<SpanEvent> Events { get; set; } = new();
}

/// <summary>
/// Trace event.
/// </summary>
public class SpanEvent
{
    public string Name { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Attributes { get; set; } = new();
}

/// <summary>
/// Trace filtering criteria.
/// </summary>
public class TraceFilter
{
    public string? ServiceName { get; set; }
    public string? OperationName { get; set; }
    public TimeSpan? MinDuration { get; set; }
    public TimeSpan? MaxDuration { get; set; }
    public SpanStatus? Status { get; set; }
    public int? Limit { get; set; }
}

/// <summary>
/// Sampling configuration.
/// </summary>
public class SamplingConfig
{
    public SamplingStrategy Strategy { get; set; }
    public double SamplingRatio { get; set; }
    public bool RecordExceptions { get; set; }
    public bool RecordEvents { get; set; }
}

/// <summary>
/// Trace statistics.
/// </summary>
public class TraceStatistics
{
    public int TotalTraces { get; set; }
    public int ErrorTraces { get; set; }
    public TimeSpan AverageDuration { get; set; }
    public TimeSpan P95Duration { get; set; }
    public TimeSpan P99Duration { get; set; }
    public double ErrorRate { get; set; }
    public int TotalSpans { get; set; }
}
