using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Observability.Interfaces;

/// <summary>
/// Prometheus metrics exporter service.
/// Exposes system and application metrics in Prometheus text format.
/// </summary>
public interface IPrometheusExporter
{
    /// <summary>
    /// Register a metric for export.
    /// </summary>
    /// <param name="metricName">Prometheus metric name (snake_case)</param>
    /// <param name="metricType">Counter, Gauge, Histogram, or Summary</param>
    /// <param name="help">Help text describing the metric</param>
    void RegisterMetric(string metricName, MetricType metricType, string help);

    /// <summary>
    /// Record a metric value.
    /// </summary>
    void RecordMetric(string metricName, double value, Dictionary<string, string>? labels = null);

    /// <summary>
    /// Increment a counter.
    /// </summary>
    void IncrementCounter(string metricName, Dictionary<string, string>? labels = null, double value = 1.0);

    /// <summary>
    /// Set a gauge value.
    /// </summary>
    void SetGauge(string metricName, double value, Dictionary<string, string>? labels = null);

    /// <summary>
    /// Record a histogram observation.
    /// </summary>
    void ObserveHistogram(string metricName, double value, Dictionary<string, string>? labels = null);

    /// <summary>
    /// Record a summary observation.
    /// </summary>
    void ObserveSummary(string metricName, double value, Dictionary<string, string>? labels = null);

    /// <summary>
    /// Export all metrics in Prometheus format.
    /// </summary>
    /// <returns>Prometheus text format metric dump</returns>
    Task<string> ExportMetricsAsync();

    /// <summary>
    /// Export metrics filtered by pattern.
    /// </summary>
    Task<string> ExportMetricsAsync(string pattern);

    /// <summary>
    /// Get metric count.
    /// </summary>
    int GetMetricCount();

    /// <summary>
    /// Get all registered metric names.
    /// </summary>
    List<string> GetMetricNames();

    /// <summary>
    /// Clear all metrics (reset).
    /// </summary>
    void ClearMetrics();

    /// <summary>
    /// Configure scrape endpoint settings.
    /// </summary>
    void ConfigureScrapeEndpoint(string path, int port);
}

/// <summary>
/// Prometheus metric types.
/// </summary>
public enum MetricType
{
    Counter,
    Gauge,
    Histogram,
    Summary
}

/// <summary>
/// Prometheus metric metadata.
/// </summary>
public class PrometheusMetricData
{
    public string Name { get; set; } = string.Empty;
    public MetricType Type { get; set; }
    public string Help { get; set; } = string.Empty;
    public double Value { get; set; }
    public Dictionary<string, string> Labels { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
