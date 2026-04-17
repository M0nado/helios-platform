using Microsoft.Extensions.Logging;

namespace HELIOS.Platform.Core.ML.Services;

/// <summary>
/// Implementation of IDataCollector - aggregates metrics from all platform services.
/// </summary>
public class DataCollector : IDataCollector
{
    private readonly ILogger<DataCollector> _logger;
    private readonly ITimeSeriesDB _timeSeriesDB;
    private readonly Dictionary<string, Func<Task<IDictionary<string, object>>>> _providers = new();
    private readonly Dictionary<string, List<MetricSnapshot>> _historicalCache = new();
    private readonly SemaphoreSlim _cacheSemaphore = new(1, 1);

    public DataCollector(ILogger<DataCollector> logger, ITimeSeriesDB timeSeriesDB)
    {
        _logger = logger;
        _timeSeriesDB = timeSeriesDB;
    }

    public async Task<IDictionary<string, object>> CollectMetricsAsync(CancellationToken cancellationToken = default)
    {
        var allMetrics = new Dictionary<string, object>();

        foreach (var (serviceName, provider) in _providers)
        {
            try
            {
                var metrics = await provider();
                allMetrics[$"{serviceName}_metrics"] = metrics;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to collect metrics from {ServiceName}", serviceName);
            }
        }

        return allMetrics;
    }

    public async Task<IDictionary<string, object>> CollectServiceMetricsAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        if (!_providers.TryGetValue(serviceName, out var provider))
        {
            _logger.LogWarning("No metric provider registered for {ServiceName}", serviceName);
            return new Dictionary<string, object>();
        }

        try
        {
            return await provider();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting metrics from {ServiceName}", serviceName);
            return new Dictionary<string, object>();
        }
    }

    public void RegisterMetricProvider(string serviceName, Func<Task<IDictionary<string, object>>> provider)
    {
        _providers[serviceName] = provider;
        _logger.LogInformation("Registered metric provider for {ServiceName}", serviceName);
    }

    public IEnumerable<string> GetRegisteredProviders() => _providers.Keys;

    public async Task<IList<MetricSnapshot>> CollectHistoricalMetricsAsync(string serviceName, TimeSpan period, CancellationToken cancellationToken = default)
    {
        await _cacheSemaphore.WaitAsync(cancellationToken);
        try
        {
            var cacheKey = $"{serviceName}_{period.TotalHours}h";

            // Return from cache if available
            if (_historicalCache.TryGetValue(cacheKey, out var cached) && cached.Any())
            {
                return cached;
            }

            // Collect current metrics
            var endTime = DateTime.UtcNow;
            var startTime = endTime.Subtract(period);
            var snapshots = new List<MetricSnapshot>();

            if (_providers.TryGetValue(serviceName, out var provider))
            {
                var metrics = await provider();
                snapshots.Add(new MetricSnapshot
                {
                    Timestamp = DateTime.UtcNow,
                    Metrics = metrics,
                    ServiceName = serviceName
                });
            }

            // Cache and return
            _historicalCache[cacheKey] = snapshots;
            return snapshots;
        }
        finally
        {
            _cacheSemaphore.Release();
        }
    }
}
