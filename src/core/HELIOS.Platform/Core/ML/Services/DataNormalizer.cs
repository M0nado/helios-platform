using Microsoft.Extensions.Logging;

namespace HELIOS.Platform.Core.ML.Services;

/// <summary>
/// Data normalization service for standardizing metrics across sources.
/// </summary>
public class DataNormalizer : IDataNormalizer
{
    private readonly ILogger<DataNormalizer> _logger;
    private readonly Dictionary<string, Func<object, Task<double>>> _rules = new();
    private readonly Dictionary<string, (double Min, double Max)> _ranges = new();

    public DataNormalizer(ILogger<DataNormalizer> logger)
    {
        _logger = logger;
        InitializeDefaultRules();
    }

    public async Task<NormalizedMetric> NormalizeMetricAsync(string metricName, object rawValue, string sourceUnit, CancellationToken cancellationToken = default)
    {
        double normalizedValue;

        if (_rules.TryGetValue(metricName, out var rule))
        {
            normalizedValue = await rule(rawValue);
        }
        else
        {
            normalizedValue = Convert.ToDouble(rawValue);
        }

        var (min, max) = _ranges.TryGetValue(metricName, out var range) 
            ? range 
            : (0.0, 100.0);

        return new NormalizedMetric
        {
            Name = metricName,
            Value = normalizedValue,
            StandardUnit = GetStandardUnit(metricName),
            Timestamp = DateTime.UtcNow,
            Min = min,
            Max = max,
            Mean = (min + max) / 2,
            IsOutlier = normalizedValue < min || normalizedValue > max
        };
    }

    public async Task<IList<NormalizedMetric>> NormalizeMetricsAsync(IDictionary<string, object> metrics, string sourceFormat, CancellationToken cancellationToken = default)
    {
        var normalized = new List<NormalizedMetric>();

        foreach (var kvp in metrics)
        {
            var metric = await NormalizeMetricAsync(kvp.Key, kvp.Value, sourceFormat, cancellationToken);
            normalized.Add(metric);
        }

        return normalized;
    }

    public (double Min, double Max) GetNormalizedRange(string metricName)
    {
        return _ranges.TryGetValue(metricName, out var range) 
            ? range 
            : (0.0, 100.0);
    }

    public void RegisterNormalizationRule(string metricName, Func<object, Task<double>> normalizer)
    {
        _rules[metricName] = normalizer;
        _logger.LogInformation("Registered normalization rule for {MetricName}", metricName);
    }

    public async Task<IList<NormalizedMetric>> HandleOutliersAsync(IList<NormalizedMetric> metrics, double threshold, CancellationToken cancellationToken = default)
    {
        if (metrics.Count < 3) return metrics;

        var byMetric = metrics.GroupBy(m => m.Name);
        var result = new List<NormalizedMetric>();

        foreach (var group in byMetric)
        {
            var values = group.Select(m => m.Value).ToList();
            var mean = values.Average();
            var stdDev = CalculateStdDev(values);

            foreach (var metric in group)
            {
                var zScore = stdDev > 0 ? Math.Abs(metric.Value - mean) / stdDev : 0;
                metric.IsOutlier = zScore > threshold;

                if (metric.IsOutlier)
                {
                    _logger.LogWarning("Detected outlier in {MetricName}: {Value} (Z-score: {ZScore})", 
                        metric.Name, metric.Value, zScore);
                    
                    // Replace with interpolated value
                    var nonOutliers = group.Where(m => !m.IsOutlier).Select(m => m.Value).ToList();
                    if (nonOutliers.Any())
                    {
                        metric.Value = nonOutliers.Average();
                    }
                }

                result.Add(metric);
            }
        }

        return await Task.FromResult(result);
    }

    #region Helper Methods

    private void InitializeDefaultRules()
    {
        // CPU percentage - keep as is (0-100)
        RegisterNormalizationRule("cpu_usage", async v => await Task.FromResult(Math.Max(0, Math.Min(100, Convert.ToDouble(v)))));
        
        // Memory in different units
        RegisterNormalizationRule("memory_mb", async v => await Task.FromResult(Convert.ToDouble(v) / 1024)); // Convert MB to GB
        RegisterNormalizationRule("memory_gb", async v => await Task.FromResult(Convert.ToDouble(v)));
        
        // Disk usage
        RegisterNormalizationRule("disk_usage_percent", async v => await Task.FromResult(Math.Max(0, Math.Min(100, Convert.ToDouble(v)))));
        
        // Network speed Mbps
        RegisterNormalizationRule("network_speed", async v => await Task.FromResult(Convert.ToDouble(v) / 1000)); // Convert to Gbps
        
        // Response time in ms
        RegisterNormalizationRule("response_time_ms", async v => await Task.FromResult(Convert.ToDouble(v)));
    }

    private string GetStandardUnit(string metricName)
    {
        return metricName.ToLower() switch
        {
            var m when m.Contains("cpu") => "%",
            var m when m.Contains("memory") => "GB",
            var m when m.Contains("disk") => "%",
            var m when m.Contains("network") => "Gbps",
            var m when m.Contains("time") => "ms",
            _ => "value"
        };
    }

    private double CalculateStdDev(IList<double> values)
    {
        if (values.Count < 2) return 0;
        var mean = values.Average();
        var variance = values.Average(x => Math.Pow(x - mean, 2));
        return Math.Sqrt(variance);
    }

    #endregion
}
