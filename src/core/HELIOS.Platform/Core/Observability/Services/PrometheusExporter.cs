using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HELIOS.Platform.Core.Observability.Interfaces;

namespace HELIOS.Platform.Core.Observability.Services;

/// <summary>
/// Prometheus metrics exporter implementation.
/// Thread-safe, in-memory metric storage with Prometheus text format export.
/// </summary>
public class PrometheusExporter : IPrometheusExporter
{
    private readonly ILogger<PrometheusExporter> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Dictionary<string, PrometheusMetricData> _metrics = new();
    private readonly Dictionary<string, (Interfaces.MetricType Type, string Help)> _metricRegistry = new();
    private string _scrapeEndpoint = "/metrics";
    private int _scrapePort = 9090;

    public PrometheusExporter(ILogger<PrometheusExporter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("Prometheus Exporter initialized");
    }

    public void RegisterMetric(string metricName, Interfaces.MetricType metricType, string help)
    {
        if (string.IsNullOrEmpty(metricName))
            throw new ArgumentException("Metric name cannot be empty", nameof(metricName));

        _metricRegistry[metricName] = (metricType, help);
        _logger.LogDebug("Registered metric: {MetricName} ({Type})", metricName, metricType);
    }

    public void RecordMetric(string metricName, double value, Dictionary<string, string>? labels = null)
    {
        _semaphore.Wait();
        try
        {
            var key = BuildMetricKey(metricName, labels);
            _metrics[key] = new PrometheusMetricData
            {
                Name = metricName,
                Value = value,
                Labels = labels ?? new(),
                CreatedAt = DateTime.UtcNow,
                Type = _metricRegistry.ContainsKey(metricName)
                    ? _metricRegistry[metricName].Type
                    : Interfaces.MetricType.Gauge
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void IncrementCounter(string metricName, Dictionary<string, string>? labels = null, double value = 1.0)
    {
        _semaphore.Wait();
        try
        {
            var key = BuildMetricKey(metricName, labels);
            if (_metrics.ContainsKey(key))
            {
                _metrics[key].Value += value;
            }
            else
            {
                _metrics[key] = new PrometheusMetricData
                {
                    Name = metricName,
                    Value = value,
                    Labels = labels ?? new(),
                    Type = Interfaces.MetricType.Counter
                };
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void SetGauge(string metricName, double value, Dictionary<string, string>? labels = null)
    {
        RecordMetric(metricName, value, labels);
    }

    public void ObserveHistogram(string metricName, double value, Dictionary<string, string>? labels = null)
    {
        RecordMetric(metricName, value, labels);
    }

    public void ObserveSummary(string metricName, double value, Dictionary<string, string>? labels = null)
    {
        RecordMetric(metricName, value, labels);
    }

    public async Task<string> ExportMetricsAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var sb = new System.Text.StringBuilder();

            foreach (var (name, (type, help)) in _metricRegistry)
            {
                sb.AppendLine($"# HELP {name} {help}");
                sb.AppendLine($"# TYPE {name} {type.ToString().ToLower()}");
            }

            foreach (var (key, metric) in _metrics.OrderBy(x => x.Key))
            {
                var line = FormatPrometheusMetric(metric);
                sb.AppendLine(line);
            }

            return sb.ToString();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<string> ExportMetricsAsync(string pattern)
    {
        await _semaphore.WaitAsync();
        try
        {
            var sb = new System.Text.StringBuilder();
            var regex = new System.Text.RegularExpressions.Regex(pattern);

            foreach (var (key, metric) in _metrics.Where(x => regex.IsMatch(x.Key)))
            {
                var line = FormatPrometheusMetric(metric);
                sb.AppendLine(line);
            }

            return sb.ToString();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public int GetMetricCount()
    {
        _semaphore.Wait();
        try
        {
            return _metrics.Count;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public List<string> GetMetricNames()
    {
        _semaphore.Wait();
        try
        {
            return _metrics.Values.Select(m => m.Name).Distinct().ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void ClearMetrics()
    {
        _semaphore.Wait();
        try
        {
            _metrics.Clear();
            _logger.LogInformation("All metrics cleared");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void ConfigureScrapeEndpoint(string path, int port)
    {
        _scrapeEndpoint = path;
        _scrapePort = port;
        _logger.LogInformation("Scrape endpoint configured: {Endpoint}:{Port}", path, port);
    }

    private string BuildMetricKey(string metricName, Dictionary<string, string>? labels)
    {
        if (labels == null || labels.Count == 0)
            return metricName;

        var labelStr = string.Join(",", labels.OrderBy(x => x.Key).Select(x => $"{x.Key}=\"{x.Value}\""));
        return $"{metricName}{{{labelStr}}}";
    }

    private string FormatPrometheusMetric(PrometheusMetricData metric)
    {
        if (metric.Labels.Count == 0)
            return $"{metric.Name} {metric.Value}";

        var labelStr = string.Join(",", metric.Labels.OrderBy(x => x.Key).Select(x => $"{x.Key}=\"{x.Value}\""));
        return $"{metric.Name}{{{labelStr}}} {metric.Value}";
    }
}

/// <summary>
/// Prometheus metric data.
/// </summary>
public class PrometheusMetricData
{
    public string Name { get; set; } = string.Empty;
    public Interfaces.MetricType Type { get; set; }
    public double Value { get; set; }
    public Dictionary<string, string> Labels { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
