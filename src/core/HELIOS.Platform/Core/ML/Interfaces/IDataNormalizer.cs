namespace HELIOS.Platform.Core.ML.Interfaces;

/// <summary>
/// Normalizes and standardizes metrics across different sources.
/// Handles unit conversion, scaling, and format transformation.
/// </summary>
public interface IDataNormalizer
{
    /// <summary>
    /// Normalizes a raw metric to standard format.
    /// </summary>
    Task<NormalizedMetric> NormalizeMetricAsync(string metricName, object rawValue, string sourceUnit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Normalizes a batch of metrics.
    /// </summary>
    Task<IList<NormalizedMetric>> NormalizeMetricsAsync(IDictionary<string, object> metrics, string sourceFormat, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the normalized range for a metric.
    /// </summary>
    (double Min, double Max) GetNormalizedRange(string metricName);

    /// <summary>
    /// Registers a custom normalization rule.
    /// </summary>
    void RegisterNormalizationRule(string metricName, Func<object, Task<double>> normalizer);

    /// <summary>
    /// Detects and handles outliers in metric values.
    /// </summary>
    Task<IList<NormalizedMetric>> HandleOutliersAsync(IList<NormalizedMetric> metrics, double threshold, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a normalized metric value.
/// </summary>
public class NormalizedMetric
{
    public string Name { get; set; } = string.Empty;
    public double Value { get; set; }
    public string StandardUnit { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public double Mean { get; set; }
    public bool IsOutlier { get; set; }
}
