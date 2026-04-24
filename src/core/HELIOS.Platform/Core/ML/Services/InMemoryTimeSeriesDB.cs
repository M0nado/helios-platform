using Microsoft.Extensions.Logging;

namespace HELIOS.Platform.Core.ML.Services;

/// <summary>
/// In-memory time-series database implementation for Phase 3.
/// Suitable for development; production should use InfluxDB or similar.
/// </summary>
public class InMemoryTimeSeriesDB : ITimeSeriesDB
{
    private readonly ILogger<InMemoryTimeSeriesDB> _logger;
    private readonly List<MetricPoint> _points = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly TimeSpan _defaultRetention = TimeSpan.FromDays(30);

    public InMemoryTimeSeriesDB(ILogger<InMemoryTimeSeriesDB> logger)
    {
        _logger = logger;
    }

    public async Task WritePointAsync(string serviceName, string metricName, double value, 
        IDictionary<string, string>? tags = null, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            _points.Add(new MetricPoint
            {
                ServiceName = serviceName,
                MetricName = metricName,
                Value = value,
                Timestamp = DateTime.UtcNow,
                Tags = tags ?? new Dictionary<string, string>()
            });
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task WriteBatchAsync(IList<MetricPoint> points, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            _points.AddRange(points);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IList<MetricPoint>> QueryAsync(string serviceName, string metricName, 
        DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return _points
                .Where(p => p.ServiceName == serviceName && p.MetricName == metricName 
                    && p.Timestamp >= start && p.Timestamp <= end)
                .OrderBy(p => p.Timestamp)
                .ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IList<AggregatedMetric>> QueryAggregatedAsync(string serviceName, string metricName, 
        DateTime start, DateTime end, TimeSpan interval, AggregationType aggregationType, 
        CancellationToken cancellationToken = default)
    {
        var rawPoints = await QueryAsync(serviceName, metricName, start, end, cancellationToken);
        var aggregated = new List<AggregatedMetric>();

        var currentStart = start;
        while (currentStart < end)
        {
            var currentEnd = currentStart.Add(interval);
            var bucketPoints = rawPoints.Where(p => p.Timestamp >= currentStart && p.Timestamp < currentEnd).ToList();

            if (bucketPoints.Any())
            {
                var values = bucketPoints.Select(p => p.Value).ToList();
                aggregated.Add(new AggregatedMetric
                {
                    Timestamp = currentStart,
                    Value = aggregationType switch
                    {
                        AggregationType.Sum => values.Sum(),
                        AggregationType.Average => values.Average(),
                        AggregationType.Max => values.Max(),
                        AggregationType.Min => values.Min(),
                        AggregationType.Count => values.Count,
                        AggregationType.StdDev => CalculateStdDev(values),
                        _ => values.Average()
                    },
                    Count = values.Count,
                    Min = values.Min(),
                    Max = values.Max(),
                    StdDev = CalculateStdDev(values)
                });
            }

            currentStart = currentEnd;
        }

        return aggregated;
    }

    public async Task PruneOldDataAsync(TimeSpan retention, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var cutoffTime = DateTime.UtcNow.Subtract(retention);
            var removed = _points.RemoveAll(p => p.Timestamp < cutoffTime);
            _logger.LogInformation("Pruned {RemovedCount} old metric points", removed);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<TimeSeriesDBStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (!_points.Any())
            {
                return new TimeSeriesDBStats
                {
                    TotalPoints = 0,
                    ServiceCount = 0,
                    StorageSizeBytes = 0
                };
            }

            return new TimeSeriesDBStats
            {
                TotalPoints = _points.Count,
                ServiceCount = _points.Select(p => p.ServiceName).Distinct().Count(),
                EarliestPoint = _points.Min(p => p.Timestamp),
                LatestPoint = _points.Max(p => p.Timestamp),
                StorageSizeBytes = _points.Count * 128, // Rough estimate
                PointsPerSecond = _points.Count / (DateTime.UtcNow - _points.Min(p => p.Timestamp)).TotalSeconds
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static double CalculateStdDev(IList<double> values)
    {
        if (values.Count < 2) return 0;
        var mean = values.Average();
        var variance = values.Average(x => Math.Pow(x - mean, 2));
        return Math.Sqrt(variance);
    }
}
