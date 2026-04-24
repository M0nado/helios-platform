namespace HELIOS.Platform.Core.ML.Interfaces;

/// <summary>
/// Manages efficient storage and retrieval of time-series metrics.
/// Provides time-based queries and data retention policies.
/// </summary>
public interface ITimeSeriesDB
{
    /// <summary>
    /// Writes a metric point to the time-series database.
    /// </summary>
    Task WritePointAsync(string serviceName, string metricName, double value, IDictionary<string, string>? tags = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes multiple metric points in batch.
    /// </summary>
    Task WriteBatchAsync(IList<MetricPoint> points, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries metrics for a time range.
    /// </summary>
    Task<IList<MetricPoint>> QueryAsync(string serviceName, string metricName, DateTime start, DateTime end, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries with aggregation (sum, avg, max, min).
    /// </summary>
    Task<IList<AggregatedMetric>> QueryAggregatedAsync(string serviceName, string metricName, DateTime start, DateTime end, 
        TimeSpan interval, AggregationType aggregationType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old data based on retention policy.
    /// </summary>
    Task PruneOldDataAsync(TimeSpan retention, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets database health and statistics.
    /// </summary>
    Task<TimeSeriesDBStats> GetStatsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a single metric data point.
/// </summary>
public class MetricPoint
{
    public string ServiceName { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
    public IDictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Aggregated metric over a time interval.
/// </summary>
public class AggregatedMetric
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public int Count { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public double StdDev { get; set; }
}

/// <summary>
/// Aggregation function types.
/// </summary>
public enum AggregationType
{
    Sum,
    Average,
    Max,
    Min,
    Count,
    StdDev
}

/// <summary>
/// Statistics about time-series database.
/// </summary>
public class TimeSeriesDBStats
{
    public long TotalPoints { get; set; }
    public int ServiceCount { get; set; }
    public DateTime EarliestPoint { get; set; }
    public DateTime LatestPoint { get; set; }
    public long StorageSizeBytes { get; set; }
    public double PointsPerSecond { get; set; }
}
