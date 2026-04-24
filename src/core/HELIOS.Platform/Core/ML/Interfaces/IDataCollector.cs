namespace HELIOS.Platform.Core.ML.Interfaces;

/// <summary>
/// Unified metrics collection service across all platform services.
/// Aggregates metrics from various sources into a normalized format.
/// </summary>
public interface IDataCollector
{
    /// <summary>
    /// Collects metrics from all active services.
    /// </summary>
    Task<IDictionary<string, object>> CollectMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Collects metrics for a specific service.
    /// </summary>
    Task<IDictionary<string, object>> CollectServiceMetricsAsync(string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a metric provider.
    /// </summary>
    void RegisterMetricProvider(string serviceName, Func<Task<IDictionary<string, object>>> provider);

    /// <summary>
    /// Gets all registered metric providers.
    /// </summary>
    IEnumerable<string> GetRegisteredProviders();

    /// <summary>
    /// Collects historical metrics for trend analysis.
    /// </summary>
    Task<IList<MetricSnapshot>> CollectHistoricalMetricsAsync(string serviceName, TimeSpan period, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a point-in-time snapshot of metrics.
/// </summary>
public class MetricSnapshot
{
    public DateTime Timestamp { get; set; }
    public IDictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
    public string ServiceName { get; set; } = string.Empty;
}
